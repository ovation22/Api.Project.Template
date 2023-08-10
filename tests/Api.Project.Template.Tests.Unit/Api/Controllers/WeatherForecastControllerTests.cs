using Api.Project.Template.Api.Controllers;
using Api.Project.Template.Core.Interfaces.Logging;
using Api.Project.Template.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Api.Controllers;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller;
    private readonly IWeatherForecastService _service;
    private readonly ILoggerAdapter<WeatherForecastController> _logger;

    public WeatherForecastControllerTests()
    {
        _service = Substitute.For<IWeatherForecastService>();
        _logger = Substitute.For<ILoggerAdapter<WeatherForecastController>>();

        _controller = new WeatherForecastController(_service, _logger);
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
        _service.GetWeatherForecast().Throws(new Exception());

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
        _service.GetWeatherForecast().Throws(ex);

        // Act
        _controller.Get();

        // Assert
        _logger.Received(1);
    }
}
