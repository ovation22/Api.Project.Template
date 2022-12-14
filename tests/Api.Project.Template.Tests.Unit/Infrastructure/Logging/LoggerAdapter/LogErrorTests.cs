using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogErrorTests
{
    private readonly Exception _exception;
    private readonly Mock<ILogger<LogErrorTests>> _loggerMock;
    private readonly LoggerAdapter<LogErrorTests> _loggerAdapter;

    public LogErrorTests()
    {
        _exception = new Exception();
        _loggerMock = new Mock<ILogger<LogErrorTests>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogErrorTests>(_loggerMock.Object);
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists");

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1}", 1);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1} {2}", 1, 2);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }
}
