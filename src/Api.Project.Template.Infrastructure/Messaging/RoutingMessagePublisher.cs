using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// Decorator for IMessagePublisher that applies message-type-based routing configuration.
/// Resolves routing from configuration and delegates to inner publisher.
/// </summary>
public class RoutingMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IMessagePublisher _innerPublisher;
    private readonly MessageRoutingConfig _routingConfig;
    private readonly ILoggerAdapter<RoutingMessagePublisher> _logger;

    // Cache resolved routing to avoid repeated lookups
    private readonly ConcurrentDictionary<Type, MessagePublishOptions> _routingCache = new();

    public RoutingMessagePublisher(
        IMessagePublisher innerPublisher,
        IOptions<MessageRoutingConfig> routingConfig,
        ILoggerAdapter<RoutingMessagePublisher> logger)
    {
        _innerPublisher = innerPublisher ?? throw new ArgumentNullException(nameof(innerPublisher));
        _routingConfig = routingConfig?.Value ?? throw new ArgumentNullException(nameof(routingConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation(
            "RoutingMessagePublisher initialized with provider: {Provider}, Routes: {RouteCount}",
            _routingConfig.Provider,
            _routingConfig.Routes.Count);
    }

    public async Task PublishAsync<T>(
        T message,
        MessagePublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        // If caller provided explicit destination, respect it (override routing config)
        if (options is { Destination: not null })
        {
            _logger.LogDebug(
                "Publishing {MessageType} with explicit destination: {Destination}",
                typeof(T).Name,
                options.Destination);

            await _innerPublisher.PublishAsync(message, options, cancellationToken);
            return;
        }

        // Resolve routing from configuration (cached)
        var resolvedOptions = GetOrCreateRoutingOptions<T>(options);

        _logger.LogDebug(
            "Publishing {MessageType} to {Destination} with routing key {RoutingKey}",
            typeof(T).Name,
            resolvedOptions.Destination,
            resolvedOptions.Subject);

        await _innerPublisher.PublishAsync(message, resolvedOptions, cancellationToken);
    }

    private MessagePublishOptions GetOrCreateRoutingOptions<T>(MessagePublishOptions? userOptions)
    {
        return _routingCache.GetOrAdd(typeof(T), _ =>
        {
            var messageTypeName = typeof(T).Name;

            // Try to find route config for this message type
            if (_routingConfig.Routes.TryGetValue(messageTypeName, out var route))
            {
                _logger.LogDebug(
                    "Resolved routing for {MessageType}: Destination={Destination}, RoutingKey={RoutingKey}",
                    messageTypeName,
                    route.Destination ?? _routingConfig.DefaultDestination,
                    route.RoutingKey ?? route.Subject ?? _routingConfig.DefaultRoutingKey ?? messageTypeName);

                return new MessagePublishOptions
                {
                    Destination = route.Destination ?? _routingConfig.DefaultDestination,
                    Subject = route.RoutingKey ?? route.Subject ?? _routingConfig.DefaultRoutingKey ?? messageTypeName,
                    Metadata = MergeMetadata(userOptions?.Metadata, route.Metadata)
                };
            }

            // Fallback to defaults
            _logger.LogWarning(
                "No routing configuration found for {MessageType}, using defaults (Destination={Destination}, RoutingKey={RoutingKey})",
                messageTypeName,
                _routingConfig.DefaultDestination ?? "(null)",
                _routingConfig.DefaultRoutingKey ?? messageTypeName);

            return new MessagePublishOptions
            {
                Destination = _routingConfig.DefaultDestination,
                Subject = _routingConfig.DefaultRoutingKey ?? messageTypeName,
                Metadata = userOptions?.Metadata
            };
        });
    }

    private static IDictionary<string, object>? MergeMetadata(
        IDictionary<string, object>? userMetadata,
        Dictionary<string, string>? routeMetadata)
    {
        if (userMetadata == null && routeMetadata == null)
            return null;

        var merged = new Dictionary<string, object>();

        // Add route metadata first (lower priority)
        if (routeMetadata != null)
        {
            foreach (var kvp in routeMetadata)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        // Add user metadata (higher priority, overrides route metadata)
        if (userMetadata != null)
        {
            foreach (var kvp in userMetadata)
            {
                merged[kvp.Key] = kvp.Value;
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_innerPublisher is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_innerPublisher is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
