using Api.Project.Template.Core.Models.Entities;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Collection("ContextFixture")]
[Trait("Category", "ContextRepository")]
public class Add : ContextRepositoryTestBase
{
    public Add(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ItAddsWeatherForecast()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = DateTime.UtcNow,
            Summary = "Pleasant",
            TemperatureC = 18
        };

        // Act
        await Repository.Add(weatherForecast);

        // Assert
        Assert.Contains(Context.WeatherForecasts, x => x == weatherForecast);
    }

    [Fact]
    public async Task ItReturnsNewlyAddedWeatherForecast()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = DateTime.UtcNow,
            Summary = "Mild",
            TemperatureC = 17
        };

        // Act
        var newlyAddedWeatherForecast = await Repository.Add(weatherForecast);

        // Assert
        Assert.Equal(weatherForecast, newlyAddedWeatherForecast);
    }
}
