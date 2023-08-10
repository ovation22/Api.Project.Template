using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogWarningTests
{
    private readonly Exception _exception;
    private readonly ILogger<LogWarningTests> _logger;
    private readonly LoggerAdapter<LogWarningTests> _loggerAdapter;

    public LogWarningTests()
    {
        _exception = new Exception();
        _logger = Substitute.For<ILogger<LogWarningTests>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogWarningTests>(_logger);
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists");

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1}", 1);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1} {2}", 1, 2);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogWarningIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning("It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenException_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists");

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenException_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1}", 1);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenException_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1} {2}", 1, 2);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenException_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogWarning(_exception, "It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }
}
