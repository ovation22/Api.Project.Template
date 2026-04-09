namespace Api.Project.Template.Worker;

/// <summary>
/// Message published by the API when a weather forecast query is executed.
/// Shape must match WeatherForecastRequestedEvent in the Application layer.
/// </summary>
public record WeatherRequested(
    int Page,
    int Size,
    DateTimeOffset RequestedAt);
