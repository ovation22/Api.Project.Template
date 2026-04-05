namespace Api.Project.Template.Domain.Entities;

public class WeatherForecast
{
    public int Id { get; set; }

    public DateOnly Date { get; init; }

    public int TemperatureC { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public int SummaryId { get; set; }

    public WeatherSummary Summary { get; set; } = default!;
}
