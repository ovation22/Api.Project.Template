using Api.Project.Template.Application.Messaging.Abstractions;
using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Events;

public class WeatherForecastRequestedEventHandler(IMessagePublisher messageBus)
    : INotificationHandler<WeatherForecastRequestedEvent>
{
    public Task Handle(WeatherForecastRequestedEvent notification, CancellationToken cancellationToken)
        => messageBus.PublishAsync(notification, cancellationToken: cancellationToken);
}
