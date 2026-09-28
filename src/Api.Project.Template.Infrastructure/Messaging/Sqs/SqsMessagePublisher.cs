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
using SnsAttribute = Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using SqsAttribute = Amazon.SQS.Model.MessageAttributeValue;

namespace Api.Project.Template.Infrastructure.Messaging.Sqs;

/// <summary>
/// AWS SQS/SNS implementation of IMessagePublisher.
/// Routes to SNS when destination is a topic ARN (arn:aws:sns:…), otherwise publishes directly to an SQS queue.
/// Every message carries MessageType, Subject, Timestamp and CorrelationId attributes, plus the publish
/// options' Metadata (as far as the 10-attribute limit allows).
/// Supports LocalStack for local development (set ConnectionStrings:sqs to the LocalStack endpoint URL);
/// without a connection string it uses the regional AWS endpoint and the default credential chain.
/// </summary>
public class SqsMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    // SQS and SNS both allow at most 10 message attributes.
    private const int MaxMessageAttributes = 10;

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

        // Explicit keys are for LocalStack (see appsettings.Development.json / the AppHost). Without them
        // the SDK's default credential chain applies (IAM role, environment variables, profile).
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

        // MessageBus:Sqs:DefaultDestination, when set, is where every SQS message goes — it takes precedence
        // over the routing Destination. The routing section is shared by all providers and its Destination is
        // typically a RabbitMQ exchange / Service Bus topic, which means nothing to SQS. Leave it empty to
        // use the routing Destination (a queue name, queue URL, or SNS topic ARN) instead.
        var destination = (!string.IsNullOrEmpty(_defaultDestination) ? _defaultDestination : null)
            ?? options?.Destination
            ?? throw new InvalidOperationException(
                "No destination specified and MessageBus:Sqs:DefaultDestination is not configured.");

        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var attributes = BuildAttributes<T>(options);

        try
        {
            if (destination.StartsWith("arn:aws:sns:", StringComparison.OrdinalIgnoreCase))
                await PublishToSnsAsync(destination, attributes["Subject"], payload, attributes, cancellationToken);
            else
                await PublishToSqsAsync(destination, payload, attributes, cancellationToken);

            _logger.LogInformation(
                "Published {MessageType} to {Destination}",
                typeof(T).Name, destination);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish {MessageType} to {Destination}",
                typeof(T).Name, destination);
            throw;
        }
    }

    /// <summary>
    /// Builds the string attributes sent with every message: the standard ones first, then Metadata
    /// until the 10-attribute limit (extra Metadata entries are dropped with a warning).
    /// </summary>
    private Dictionary<string, string> BuildAttributes<T>(MessagePublishOptions? options)
    {
        var attributes = new Dictionary<string, string>
        {
            ["MessageType"] = typeof(T).FullName ?? typeof(T).Name,
            ["Subject"] = options?.Subject ?? typeof(T).Name,
            ["Timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["CorrelationId"] = CorrelationId.Current()
        };

        foreach (var (key, value) in options?.Metadata ?? new Dictionary<string, object>())
        {
            // Attribute values can't be empty; standard attributes win over Metadata with the same name
            var text = value?.ToString();
            if (string.IsNullOrEmpty(text) || attributes.ContainsKey(key))
                continue;

            if (attributes.Count == MaxMessageAttributes)
            {
                _logger.LogWarning("Dropping metadata {Key}: SQS/SNS allow at most {Max} message attributes", key, MaxMessageAttributes);
                continue;
            }

            attributes[key] = text;
        }

        return attributes;
    }

    private async Task PublishToSnsAsync(
        string topicArn, string subject, string payload, Dictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        var request = new PublishRequest
        {
            TopicArn = topicArn,
            Message = payload,
            Subject = subject,
            MessageAttributes = attributes.ToDictionary(
                kvp => kvp.Key,
                kvp => new SnsAttribute { DataType = "String", StringValue = kvp.Value })
        };

        await _snsClient.PublishAsync(request, cancellationToken);
    }

    private async Task PublishToSqsAsync(
        string destination, string payload, Dictionary<string, string> attributes,
        CancellationToken cancellationToken)
    {
        var queueUrl = await ResolveQueueUrlAsync(destination, cancellationToken);

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = payload,
            MessageAttributes = attributes.ToDictionary(
                kvp => kvp.Key,
                kvp => new SqsAttribute { DataType = "String", StringValue = kvp.Value })
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
