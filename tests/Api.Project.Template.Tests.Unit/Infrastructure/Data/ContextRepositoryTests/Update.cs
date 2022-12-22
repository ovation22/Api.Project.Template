using Api.Project.Template.Core.Models.Entities;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Collection("ContextFixture")]
[Trait("Category", "ContextRepository")]
public class Update : ContextRepositoryTestBase
{
    private readonly WeatherForecast _weatherForecast;

    public Update(ContextFixture fixture) : base(fixture)
    {
        _weatherForecast = new WeatherForecast
        {
            Id = new Guid("5FD2E324-A935-484E-8F9F-F52E7921EF21"),
            Date = DateTime.UtcNow,
            Summary = "Pleasant",
            TemperatureC = 18
        };
        Context.WeatherForecasts.Add(_weatherForecast);
        Context.SaveChanges();
    }

    [Fact]
    public async Task ItUpdatesWeatherForecast()
    {
        // Arrange
        _weatherForecast.Summary = "Updated";

        // Act
        await Repository.Update(_weatherForecast);

        // Assert
        Assert.Contains(Context.WeatherForecasts, x => x.Summary == "Updated");
    }
}
