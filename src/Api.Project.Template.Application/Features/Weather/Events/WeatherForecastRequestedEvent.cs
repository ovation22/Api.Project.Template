using MediatR;

namespace Api.Project.Template.Application.Features.Weather.Events;

public record WeatherForecastRequestedEvent(
    int Page,
    int Size,
    DateTimeOffset RequestedAt) : INotification;
