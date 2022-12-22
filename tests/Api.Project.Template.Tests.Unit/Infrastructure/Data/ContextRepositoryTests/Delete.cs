using Api.Project.Template.Core.Models.Entities;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Collection("ContextFixture")]
[Trait("Category", "ContextRepository")]
public class Delete : ContextRepositoryTestBase
{
    private readonly WeatherForecast _weatherForecast;

    public Delete(ContextFixture fixture) : base(fixture)
    {
        _weatherForecast = new WeatherForecast
        {
            Id = new Guid("D6012CB6-6184-4AB4-BE14-B29C61F2CB32"),
            Date = DateTime.UtcNow,
            Summary = "Pleasant",
            TemperatureC = 18
        };
        Context.WeatherForecasts.Add(_weatherForecast);
        Context.SaveChanges();
    }

    [Fact]
    public async Task ItRemovesWeatherForecast()
    {
        // Arrange
        // Act
        await Repository.Delete(_weatherForecast);

        // Assert
        Assert.DoesNotContain(Context.WeatherForecasts, x => x == _weatherForecast);
    }
}
