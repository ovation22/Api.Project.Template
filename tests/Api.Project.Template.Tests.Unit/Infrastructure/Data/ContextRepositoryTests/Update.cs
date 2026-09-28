using Api.Project.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Update : ContextRepositoryTestBase
{
    public Update(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_PersistsChange()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 1),
            TemperatureC = 18,
            SummaryId = 1
        };
        Context.WeatherForecasts.Add(weatherForecast);
        Context.SaveChanges();

        weatherForecast.SummaryId = 2;

        // Act
        await Repository.UpdateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        // AsNoTracking reads the stored row; a tracked query would return the already-modified instance.
        var stored = await Context.WeatherForecasts.AsNoTracking()
            .SingleAsync(x => x.Id == weatherForecast.Id, TestContext.Current.CancellationToken);
        Assert.Equal(2, stored.SummaryId);
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_ReturnsUpdatedEntity()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 2),
            TemperatureC = 20,
            SummaryId = 1
        };
        Context.WeatherForecasts.Add(weatherForecast);
        Context.SaveChanges();

        weatherForecast.SummaryId = 3;

        // Act
        var result = await Repository.UpdateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(weatherForecast, result);
        Assert.Equal(3, result.SummaryId);
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_PersistsChangesToAllEntities()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 1 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 1 }
        };
        Context.WeatherForecasts.AddRange(forecasts);
        Context.SaveChanges();

        foreach (var f in forecasts)
            f.SummaryId = 4;

        // Act
        await Repository.UpdateAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        var ids = forecasts.Select(f => f.Id).ToList();
        var stored = await Context.WeatherForecasts.AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, stored.Count);
        Assert.All(stored, x => Assert.Equal(4, x.SummaryId));
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_ReturnsAllUpdatedEntities()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 1 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 1 }
        };
        Context.WeatherForecasts.AddRange(forecasts);
        Context.SaveChanges();

        foreach (var f in forecasts)
            f.SummaryId = 5;

        // Act
        var result = await Repository.UpdateAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(forecasts, result);
        Assert.All(result, f => Assert.Equal(5, f.SummaryId));
    }
}
