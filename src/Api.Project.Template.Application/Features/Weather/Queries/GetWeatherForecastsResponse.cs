namespace Api.Project.Template.Application.Features.Weather.Queries;

public record GetWeatherForecastsResponse(
    DateOnly Date,
    int TemperatureC,
    int TemperatureF,
    string Summary);
