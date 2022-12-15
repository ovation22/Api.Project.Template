using Api.Project.Template.Api.Controllers;
using Api.Project.Template.Core.Interfaces.Logging;
using Moq;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Api.Controllers;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller;
    private Mock<ILoggerAdapter<WeatherForecastController>> _loggerMock;

    public WeatherForecastControllerTests()
    {
        _loggerMock = new Mock<ILoggerAdapter<WeatherForecastController>>();

        _controller = new WeatherForecastController(_loggerMock.Object);
    }

    [Fact]
    public void ItExists()
    {
        // Arrange
        // Act
        // Assert
        _controller.Get();
    }
}
