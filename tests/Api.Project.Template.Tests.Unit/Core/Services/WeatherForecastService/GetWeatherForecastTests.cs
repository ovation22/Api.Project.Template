using Api.Project.Template.Core.Models.DTO;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Core.Services.WeatherForecastService;

public class GetWeatherForecastTests
{
    private readonly Template.Core.Services.WeatherForecastService _weatherForecastService;

    public GetWeatherForecastTests()
    {
        _weatherForecastService = new Template.Core.Services.WeatherForecastService();
    }

    [Fact]
    public void WhenCalled_ThenWeatherForecastReturned()
    {
        // Arrange
        // Act
        var result = _weatherForecastService.GetWeatherForecast();

        // Assert
        Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(result);
    }
}
