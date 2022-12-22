using Api.Project.Template.Core.Models.Entities;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Collection("ContextFixture")]
[Trait("Category", "ContextRepository")]
public class GetAll : ContextRepositoryTestBase
{
    public GetAll(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ItReturnsAllWeatherForecast()
    {
        // Arrange
        // Act
        var weatherForecasts = (List<WeatherForecast>)await Repository.GetAll<WeatherForecast>();

        // Assert
        Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(weatherForecasts);
        Assert.Equal(Context.WeatherForecasts.Count(), weatherForecasts.Count);
    }
}
