namespace Api.Project.Template.Domain.Entities;

public class WeatherSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public int MinTemperatureC { get; set; }

    public int MaxTemperatureC { get; set; }

    public ICollection<WeatherForecast> Forecasts { get; set; } = [];
}
