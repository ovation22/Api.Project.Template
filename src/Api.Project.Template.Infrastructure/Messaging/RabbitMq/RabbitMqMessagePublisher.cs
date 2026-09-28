using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
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

    // One publish channel, created and used only while holding _publishLock (channels are not thread-safe).
    private IChannel? _publishChannel;
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    // Exchanges declared on the current connection (a route may publish to an exchange other than _exchange).
    private readonly HashSet<string> _declaredExchanges = [];
    private bool _disposed;

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

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true }) return;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true }) return;

            _logger.LogInformation("Creating RabbitMQ connection...");

            // CreateConnectionAsync can throw; allow caller to handle/log and possibly retry.
            _connection = await _factory.CreateConnectionAsync(cancellationToken);

            _logger.LogInformation("RabbitMQ connection established (node: {Node})", _connection.Endpoint.HostName);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Returns an open publish channel, replacing a closed one. Must be called while holding _publishLock.
    /// </summary>
    private async Task<IChannel> GetPublishChannelAsync(CancellationToken cancellationToken)
    {
        if (_publishChannel is { IsOpen: true })
            return _publishChannel;

        if (_publishChannel != null)
        {
            try { _publishChannel.Dispose(); } catch { /* already closed */ }
        }

        _logger.LogInformation("Creating dedicated RabbitMQ publisher channel...");
        _publishChannel = await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);

        // A new channel may follow a new connection; re-declare exchanges before use.
        _declaredExchanges.Clear();
        return _publishChannel;
    }

    /// <summary>
    /// Declares the exchange once per connection (idempotent on the broker). Publishing to an undeclared
    /// exchange makes the broker close the channel with a 404. Must be called while holding _publishLock.
    /// </summary>
    private async Task EnsureExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken)
    {
        if (exchange.Length == 0 || _declaredExchanges.Contains(exchange))
            return;

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        _declaredExchanges.Add(exchange);
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
        var correlationId = CorrelationId.Current();

        var headers = new Dictionary<string, object?>();
        foreach (var (key, value) in options?.Metadata ?? new Dictionary<string, object>())
            headers[key] = value?.ToString() is { } text ? Encoding.UTF8.GetBytes(text) : null;
        headers["correlation-id"] = Encoding.UTF8.GetBytes(correlationId);
        headers["message-type"] = Encoding.UTF8.GetBytes(typeof(T).FullName ?? typeof(T).Name);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            CorrelationId = correlationId,
            Headers = headers,
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
                await EnsureConnectedAsync(cancellationToken);

                await _publishLock.WaitAsync(cancellationToken);
                try
                {
                    var channel = await GetPublishChannelAsync(cancellationToken);
                    await EnsureExchangeAsync(channel, ex, cancellationToken);

                    await channel.BasicPublishAsync(
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller cancelled — not a publish failure, so no error log and no retry
                throw;
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
        await _publishLock.WaitAsync();
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
                    _declaredExchanges.Clear();
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
            _publishLock.Release();
        }
    }

    /// <summary>
    /// Disposes resources asynchronously. Prefer this over Dispose() to avoid potential deadlocks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // Disposed by both RoutingMessagePublisher and the DI container; only the first call does work.
        if (_disposed) return;
        _disposed = true;

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
        if (_disposed) return;
        _disposed = true;

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
