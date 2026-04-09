using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Api.Project.Template.Infrastructure.Messaging.RabbitMq;

public class RabbitMqMessagePublisher : IMessagePublisher, IAsyncDisposable, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly ILoggerAdapter<RabbitMqMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _exchange;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly int _maxPublishRetries;
    private readonly TimeSpan _initialRetryDelay;

    // Channel pooling for performance optimization (Phase 2)
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public RabbitMqMessagePublisher(IConfiguration configuration, ILoggerAdapter<RabbitMqMessagePublisher> logger)
    {
        _logger = logger;

        // serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // read configuration (support several shapes used by Aspire)
        string? connectionString =
            configuration["MessageBus:RabbitMq:ConnectionString"]
            ?? configuration["MessageBus:RabbitMq"]
            ?? configuration["MessageBus__RabbitMq__ConnectionString"]
            ?? configuration["MessageBus__RabbitMq"]
            ?? configuration.GetConnectionString("RabbitMq")
            ?? configuration.GetConnectionString("messaging");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("RabbitMQ connection string not configured. Set MessageBus:RabbitMq or ConnectionStrings:RabbitMq.");

        _factory = RabbitMqConnectionStringParser.Parse(connectionString);

        // resilience settings
        _factory.AutomaticRecoveryEnabled = true;
        _factory.TopologyRecoveryEnabled = true;
        _factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
        _factory.RequestedHeartbeat = TimeSpan.FromSeconds(30);

        // publish defaults - tune via config if desired
        _exchange = configuration["MessageBus:Exchange"] ?? configuration["MessageBus__Exchange"] ?? "apiprojecttemplate.events";
        _maxPublishRetries = int.TryParse(configuration["MessageBus:Publish:MaxRetries"], out var mr) ? mr : 3;
        _initialRetryDelay = TimeSpan.FromMilliseconds(int.TryParse(configuration["MessageBus:Publish:InitialDelayMs"], out var id) ? id : 200);

        _logger.LogInformation("RabbitMqMessagePublisher configured for exchange {Exchange}", _exchange);
    }

    private async Task EnsureConnectedAsync()
    {
        if (_connection is { IsOpen: true }) return;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true }) return;

            _logger.LogInformation("Creating RabbitMQ connection...");

            // CreateConnectionAsync can throw; allow caller to handle/log and possibly retry.
            _connection = await _factory.CreateConnectionAsync();

            _logger.LogInformation("RabbitMQ connection established (node: {Node})", _connection.Endpoint.HostName);

            // Ensure exchange exists using a short-lived channel
            var channel = await _connection.CreateChannelAsync();
            try
            {
                await channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
            }
            finally
            {
                await channel.DisposeAsync();
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Ensures a dedicated publish channel is available for message publishing.
    /// Channel creation uses lock-free double-checked pattern for performance.
    /// The actual channel usage is protected by _publishLock in PublishAsync.
    /// </summary>
    private async Task EnsurePublishChannelAsync(CancellationToken cancellationToken = default)
    {
        // Fast path: channel exists and is open
        if (_publishChannel is { IsOpen: true })
            return;

        // Slow path: need to create channel (only happens once, or after connection loss)
        // Use Interlocked pattern for lock-free channel creation
        var currentChannel = _publishChannel;
        if (currentChannel is not { IsOpen: true })
        {
            // Close existing channel if it exists but is not open
            if (currentChannel != null)
            {
                try { await currentChannel.CloseAsync(cancellationToken); } catch { }
                try { currentChannel.Dispose(); } catch { }
            }

            _logger.LogInformation("Creating dedicated RabbitMQ publisher channel...");
            var newChannel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);

            // Atomic swap - if another thread created a channel, use theirs and dispose ours
            var originalChannel = Interlocked.CompareExchange(ref _publishChannel, newChannel, currentChannel);
            if (originalChannel != currentChannel && originalChannel is { IsOpen: true })
            {
                try { await newChannel.CloseAsync(cancellationToken); } catch { }
                try { newChannel.Dispose(); } catch { }
                _logger.LogInformation("Another thread created the channel first, using theirs");
            }
            else
            {
                _logger.LogInformation("Publisher channel created and ready for use");
            }
        }
    }

    public async Task PublishAsync<T>(T message, MessagePublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        cancellationToken.ThrowIfCancellationRequested();

        var ex = options?.Destination ?? _exchange;
        var rk = options?.Subject ?? typeof(T).Name;

        // Serialize message BEFORE acquiring lock (minimize lock duration)
        var payload = JsonSerializer.Serialize(message, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(payload);

        // Prepare message properties BEFORE acquiring lock
        var correlationId = Activity.Current?.Tags.FirstOrDefault(t => t.Key == "client.correlation_id").Value
                            ?? Activity.Current?.Baggage.FirstOrDefault(kv => kv.Key == "client.correlation_id").Value
                            ?? Activity.Current?.TraceId.ToString()
                            ?? Activity.Current?.Id
                            ?? Guid.NewGuid().ToString();

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            CorrelationId = correlationId,
            Headers = new Dictionary<string, object?>
            {
                ["correlation-id"] = Encoding.UTF8.GetBytes(correlationId),
                ["message-type"] = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name)
            },
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        var attempt = 0;
        var delay = _initialRetryDelay;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            try
            {
                await EnsureConnectedAsync();
                await EnsurePublishChannelAsync(cancellationToken);

                // Acquire lock to use the shared channel (channels are NOT thread-safe)
                await _publishLock.WaitAsync(cancellationToken);
                try
                {
                    await _publishChannel!.BasicPublishAsync(
                        exchange: ex,
                        routingKey: rk,
                        mandatory: false,
                        basicProperties: props,
                        body: body,
                        cancellationToken: cancellationToken);
                }
                finally
                {
                    _publishLock.Release();
                }

                _logger.LogInformation("Published message {Type} to exchange {Exchange} with routing key {RoutingKey}", typeof(T).Name, ex, rk);
                return;
            }
            catch (OperationInterruptedException oex)
            {
                _logger.LogWarning(oex, "Transient RabbitMQ interruption while publishing message {Type} attempt {Attempt}", typeof(T).Name, attempt);

                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                // connection may be in an error state – dispose and allow recreation on next loop
                await SafeCloseConnectionAsync();
            }
            catch (BrokerUnreachableException bue)
            {
                _logger.LogWarning(bue, "Broker unreachable while publishing message {Type} attempt {Attempt}", typeof(T).Name, attempt);

                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                await SafeCloseConnectionAsync();
            }
            catch (Exception exn)
            {
                _logger.LogError(exn, "Failed to publish message {Type} attempt {Attempt}", typeof(T).Name, attempt);
                // If last attempt, rethrow; otherwise back off and retry
                if (attempt >= _maxPublishRetries)
                    throw;

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 5000));
                await SafeCloseConnectionAsync();
            }
        }
    }

    private async Task SafeCloseConnectionAsync()
    {
        try
        {
            // Close publish channel first
            if (_publishChannel is not null)
            {
                try
                {
                    if (_publishChannel.IsOpen)
                    {
                        await _publishChannel.CloseAsync();
                    }
                    _publishChannel.Dispose();
                }
                catch { /* swallow */ }
                finally
                {
                    _publishChannel = null;
                }
            }

            // Then close connection
            if (_connection is not null)
            {
                if (_connection.IsOpen)
                {
                    try { await _connection.CloseAsync(); } catch { }
                }

                _connection.Dispose();
            }
        }
        catch { /* swallow */ }
        finally
        {
            _connection = null;
        }
    }

    /// <summary>
    /// Disposes resources asynchronously. Prefer this over Dispose() to avoid potential deadlocks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await SafeCloseConnectionAsync();
        _connectionLock.Dispose();
        _publishLock.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Synchronous disposal. Note: This blocks on async cleanup which may cause issues in some contexts.
    /// Prefer DisposeAsync() when possible.
    /// </summary>
    public void Dispose()
    {
        // We must block here since IDisposable.Dispose is synchronous
        // This is safe in most contexts but could deadlock in ASP.NET synchronization contexts
        // Callers should prefer DisposeAsync when possible
        try
        {
            SafeCloseConnectionAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _connectionLock.Dispose();
            _publishLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}