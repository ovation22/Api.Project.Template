using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging.Abstractions;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Events;

/// <summary>
/// Publishes the event to the message bus. Publishing is best-effort: the event is informational, so a
/// broker outage is logged rather than failing the request that raised it. (For events that must not be
/// lost, use a transactional outbox instead.)
/// </summary>
public class WeatherForecastRequestedEventHandler(
    IMessagePublisher messageBus,
    ILoggerAdapter<WeatherForecastRequestedEventHandler> logger)
    : INotificationHandler<WeatherForecastRequestedEvent>
{
    public async Task Handle(WeatherForecastRequestedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await messageBus.PublishAsync(notification, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not publish {EventType}; continuing without it", nameof(WeatherForecastRequestedEvent));
        }
    }
}
