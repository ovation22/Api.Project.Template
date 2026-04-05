using Api.Project.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Infrastructure.Data.Extensions;

public static class ModelBuilderExtensions
{
    public static void Seed(this ModelBuilder modelBuilder)
    {
        var summaryData = new[]
        {
            (Id: 1,  Name: "Freezing",   Min: -20, Max:  0),
            (Id: 2,  Name: "Bracing",    Min:   0, Max:  5),
            (Id: 3,  Name: "Chilly",     Min:   5, Max: 10),
            (Id: 4,  Name: "Cool",       Min:  10, Max: 15),
            (Id: 5,  Name: "Mild",       Min:  15, Max: 20),
            (Id: 6,  Name: "Warm",       Min:  20, Max: 25),
            (Id: 7,  Name: "Balmy",      Min:  25, Max: 30),
            (Id: 8,  Name: "Hot",        Min:  30, Max: 35),
            (Id: 9,  Name: "Sweltering", Min:  35, Max: 40),
            (Id: 10, Name: "Scorching",  Min:  40, Max: 45)
        };

        var weatherSummaries = summaryData.Select(s => new WeatherSummary
        {
            Id = s.Id,
            Name = s.Name,
            MinTemperatureC = s.Min,
            MaxTemperatureC = s.Max
        }).ToArray();

        modelBuilder.Entity<WeatherSummary>().HasData(weatherSummaries);

        var forecasts = new List<WeatherForecast>();
        var random = new Random(42); // Fixed seed for reproducibility
        var baseDate = new DateOnly(2025, 1, 5);
        var id = 1;

        foreach (var (summaryId, _, min, max) in summaryData)
        {
            for (var i = 0; i < 5; i++)
            {
                forecasts.Add(new WeatherForecast
                {
                    Id = id++,
                    Date = baseDate.AddDays(-i),
                    TemperatureC = random.Next(min, max),
                    SummaryId = summaryId
                });
            }
        }

        modelBuilder.Entity<WeatherForecast>().HasData(forecasts);
    }
}
