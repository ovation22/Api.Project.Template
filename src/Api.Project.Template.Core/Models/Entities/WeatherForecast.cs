using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Project.Template.Core.Models.Entities;

public class WeatherForecast
{
    [Key]
    public Guid Id { get; set; }

    public DateTime Date { get; init; }

    public int TemperatureC { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string Summary { get; set; } = default!;
}
