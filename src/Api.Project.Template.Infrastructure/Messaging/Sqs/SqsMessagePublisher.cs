using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.Sqs;

/// <summary>
/// AWS SQS/SNS implementation of IMessagePublisher.
/// Routes to SNS when destination is a topic ARN (arn:aws:sns:…), otherwise publishes directly to an SQS queue URL.
/// Supports LocalStack for local development (set ConnectionStrings:sqs to the LocalStack endpoint URL).
/// </summary>
public class SqsMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly AmazonSimpleNotificationServiceClient _snsClient;
    private readonly ILoggerAdapter<SqsMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
    private readonly string? _defaultDestination;
    private readonly ConcurrentDictionary<string, string> _queueUrlCache = new();

    public SqsMessagePublisher(
        IConfiguration configuration,
        ILoggerAdapter<SqsMessagePublisher> logger)
    {
        _logger = logger;

        var serviceUrl = configuration.GetConnectionString("sqs");
        var region = configuration["MessageBus:Sqs:Region"] ?? configuration["AWS:Region"] ?? "us-east-1";
        var regionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);

        var sqsConfig = new AmazonSQSConfig { RegionEndpoint = regionEndpoint };
        var snsConfig = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = regionEndpoint };

        if (!string.IsNullOrEmpty(serviceUrl))
        {
            sqsConfig.ServiceURL = serviceUrl;
            snsConfig.ServiceURL = serviceUrl;
        }

        var accessKey = configuration["AWS:AccessKey"];
        var secretKey = configuration["AWS:SecretKey"];

        if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
        {
            var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            _sqsClient = new AmazonSQSClient(credentials, sqsConfig);
            _snsClient = new AmazonSimpleNotificationServiceClient(credentials, snsConfig);
        }
        else
        {
            _sqsClient = new AmazonSQSClient(sqsConfig);
            _snsClient = new AmazonSimpleNotificationServiceClient(snsConfig);
        }

        _defaultDestination = configuration["MessageBus:Sqs:DefaultDestination"];

        _logger.LogInformation(
            "SqsMessagePublisher configured (Region: {Region}, DefaultDestination: {Destination})",
            region, _defaultDestination ?? "(none)");
    }

    public async Task PublishAsync<T>(
        T message,
        MessagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        // SQS is direct-queue — publisher and consumer must use the same queue.
        // Unlike RabbitMQ (exchange+binding) there is no routing indirection, so
        // MessageBus:Sqs:DefaultDestination takes precedence over the shared routing
        // Destination (which maps to an exchange name for RabbitMQ).
        var destination = (!string.IsNullOrEmpty(_defaultDestination) ? _defaultDestination : null)
            ?? options?.Destination
            ?? throw new InvalidOperationException(
                "No destination specified and MessageBus:Sqs:DefaultDestination is not configured.");

        var subject = options?.Subject ?? typeof(T).Name;
        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var messageTypeName = typeof(T).FullName ?? typeof(T).Name;

        try
        {
            if (destination.StartsWith("arn:aws:sns:", StringComparison.OrdinalIgnoreCase))
                await PublishToSnsAsync(destination, subject, payload, messageTypeName, cancellationToken);
            else
                await PublishToSqsAsync(destination, subject, payload, messageTypeName, cancellationToken);

            _logger.LogInformation(
                "Published {MessageType} to {Destination}",
                typeof(T).Name, destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {MessageType} to {Destination}",
                typeof(T).Name, destination);
            throw;
        }
    }

    private async Task PublishToSnsAsync(
        string topicArn, string subject, string payload, string messageType,
        CancellationToken cancellationToken)
    {
        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = payload,
            Subject = subject,
            MessageAttributes =
            {
                ["MessageType"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue { DataType = "String", StringValue = messageType },
                ["Timestamp"] = new Amazon.SimpleNotificationService.Model.MessageAttributeValue { DataType = "String", StringValue = DateTimeOffset.UtcNow.ToString("O") }
            }
        };

        await _snsClient.PublishAsync(request, cancellationToken);
    }

    private async Task PublishToSqsAsync(
        string destination, string subject, string payload, string messageType,
        CancellationToken cancellationToken)
    {
        var queueUrl = await ResolveQueueUrlAsync(destination, cancellationToken);

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = payload,
            MessageAttributes =
            {
                ["MessageType"] = new Amazon.SQS.Model.MessageAttributeValue { DataType = "String", StringValue = messageType },
                ["Subject"] = new Amazon.SQS.Model.MessageAttributeValue { DataType = "String", StringValue = subject },
                ["Timestamp"] = new Amazon.SQS.Model.MessageAttributeValue { DataType = "String", StringValue = DateTimeOffset.UtcNow.ToString("O") }
            }
        };

        await _sqsClient.SendMessageAsync(request, cancellationToken);
    }

    private async Task<string> ResolveQueueUrlAsync(string destination, CancellationToken cancellationToken)
    {
        if (_queueUrlCache.TryGetValue(destination, out var cached))
            return cached;

        // Already a full URL — use as-is
        if (destination.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _queueUrlCache[destination] = destination;
            return destination;
        }

        // Treat as queue name — SQS only allows alphanumeric, hyphens, underscores.
        // Exchange-style names (e.g. "apiprojecttemplate.events") are sanitized automatically.
        var queueName = destination.Replace('.', '-').Replace('/', '-');
        var response = await _sqsClient.CreateQueueAsync(queueName, cancellationToken);
        _queueUrlCache[destination] = response.QueueUrl;
        return response.QueueUrl;
    }

    public ValueTask DisposeAsync()
    {
        _sqsClient.Dispose();
        _snsClient.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
