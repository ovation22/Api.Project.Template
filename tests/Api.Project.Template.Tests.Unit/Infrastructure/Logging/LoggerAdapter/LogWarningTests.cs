using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogWarningTests
{
    private readonly Exception _exception;
    private readonly Mock<ILogger<LogWarningTests>> _loggerMock;
    private readonly LoggerAdapter<LogWarningTests> _loggerAdapter;

    public LogWarningTests()
    {
        _exception = new Exception();
        _loggerMock = new Mock<ILogger<LogWarningTests>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogWarningTests>(_loggerMock.Object);
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists");

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1}", 1);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1} {2}", 1, 2);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenException_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists");

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenException_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1}", 1);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenException_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1} {2}", 1, 2);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }

    [Fact]
    public void GivenException_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        _loggerMock.Verify();
        Assert.Equal(2, _loggerMock.Invocations.Count);
    }
}
