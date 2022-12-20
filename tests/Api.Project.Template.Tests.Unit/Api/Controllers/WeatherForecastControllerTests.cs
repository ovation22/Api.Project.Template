using Api.Project.Template.Api.Controllers;
using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Api.Controllers;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller;
    private readonly Mock<IWeatherForecastService> _serviceMock;
    private readonly Mock<ILoggerAdapter<WeatherForecastController>> _loggerMock;

    public WeatherForecastControllerTests()
    {
        _serviceMock = new Mock<IWeatherForecastService>();
        _loggerMock = new Mock<ILoggerAdapter<WeatherForecastController>>();

        _controller = new WeatherForecastController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ItExists()
    {
        // Arrange
        // Act
        // Assert
        _controller.Get();
    }

    [Fact]
    public void WhenException_ThenProblemDetails()
    {
        // Arrange
        _serviceMock.Setup(x => x.GetWeatherForecast()).Throws(new Exception());

        // Act
        var result = _controller.Get();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public void WhenException_ThenLogError()
    {
        // Arrange
        var ex = new Exception();
        _serviceMock.Setup(x => x.GetWeatherForecast()).Throws(ex);

        // Act
        _controller.Get();

        // Assert
        _loggerMock.Verify(x => x.LogError(ex, ex.Message), Times.Once);
    }
}
