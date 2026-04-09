using Api.Project.Template.Domain.Entities;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class GetAll : ContextRepositoryTestBase
{
    public GetAll(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ItReturnsAllWeatherForecasts()
    {
        // Arrange
        // Act
        var weatherForecasts = (await Repository.GetAllAsync<WeatherForecast>(TestContext.Current.CancellationToken)).ToList();

        // Assert
        Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(weatherForecasts);
        Assert.Equal(Context.WeatherForecasts.Count(), weatherForecasts.Count);
    }
}
