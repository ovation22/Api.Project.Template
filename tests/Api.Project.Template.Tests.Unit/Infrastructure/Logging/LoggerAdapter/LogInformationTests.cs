using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogInformationTests
{
    private readonly Exception _exception;
    private readonly Mock<ILogger<LogInformationTests>> _loggerMock;
    private readonly LoggerAdapter<LogInformationTests> _loggerAdapter;

    public LogInformationTests()
    {
        _exception = new Exception();
        _loggerMock = new Mock<ILogger<LogInformationTests>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogInformationTests>(_loggerMock.Object);
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists");

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1}", 1);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1} {2}", 1, 2);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }
}
