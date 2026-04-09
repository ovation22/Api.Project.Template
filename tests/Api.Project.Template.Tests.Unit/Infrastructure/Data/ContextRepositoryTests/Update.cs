using Api.Project.Template.Domain.Entities;

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
        Assert.Contains(Context.WeatherForecasts, x => x.Id == weatherForecast.Id && x.SummaryId == 2);
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
        Assert.All(forecasts, f =>
            Assert.Contains(Context.WeatherForecasts, x => x.Id == f.Id && x.SummaryId == 4));
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
