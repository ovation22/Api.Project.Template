using Api.Project.Template.Domain.Entities;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Delete : ContextRepositoryTestBase
{
    public Delete(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_RemovesEntity()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 1),
            TemperatureC = 18,
            SummaryId = 5
        };
        Context.WeatherForecasts.Add(weatherForecast);
        Context.SaveChanges();

        // Act
        await Repository.DeleteAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(Context.WeatherForecasts, x => x == weatherForecast);
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_DoesNotRemoveOtherEntities()
    {
        // Arrange
        var toDelete = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 1),
            TemperatureC = 18,
            SummaryId = 5
        };
        var toRetain = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 2),
            TemperatureC = 20,
            SummaryId = 5
        };
        Context.WeatherForecasts.AddRange(toDelete, toRetain);
        Context.SaveChanges();

        // Act
        await Repository.DeleteAsync(toDelete, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(Context.WeatherForecasts, x => x == toRetain);
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_RemovesAllEntities()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 6 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 6 }
        };
        Context.WeatherForecasts.AddRange(forecasts);
        Context.SaveChanges();

        // Act
        await Repository.DeleteAsync(forecasts, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(forecasts, f => Assert.DoesNotContain(Context.WeatherForecasts, x => x == f));
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_DoesNotRemoveOtherEntities()
    {
        // Arrange
        var toDelete = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 6 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 6 }
        };
        var toRetain = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 3),
            TemperatureC = 14,
            SummaryId = 7
        };
        Context.WeatherForecasts.AddRange(toDelete);
        Context.WeatherForecasts.Add(toRetain);
        Context.SaveChanges();

        // Act
        await Repository.DeleteAsync(toDelete, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(Context.WeatherForecasts, x => x == toRetain);
    }
}
