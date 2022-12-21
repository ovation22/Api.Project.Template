using Xunit;

namespace Api.Project.Template.Tests.Unit.Core.Services.WeatherForecast;

public class GetWeatherForecastTests
{
    private readonly Template.Core.Services.WeatherForecastService _service;

    public GetWeatherForecastTests()
    {
        _service = new Template.Core.Services.WeatherForecastService();
    }

    [Fact]
    public void WhenCalled_ThenWeatherForecastReturned()
    {
        // Arrange
        // Act
        var result = _service.GetWeatherForecast();

        // Assert
        Assert.IsAssignableFrom<IEnumerable<Template.Core.Models.DTO.WeatherForecast>>(result);
    }
}
