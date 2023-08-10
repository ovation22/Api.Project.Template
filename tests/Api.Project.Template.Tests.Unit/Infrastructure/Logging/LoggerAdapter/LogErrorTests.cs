using Microsoft.Extensions.Logging;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;
using NSubstitute;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogErrorTests
{
    private readonly Exception _exception;
    private readonly ILogger<LogErrorTests> _logger;
    private readonly LoggerAdapter<LogErrorTests> _loggerAdapter;

    public LogErrorTests()
    {
        _exception = new Exception();
        _logger = Substitute.For<ILogger<LogErrorTests>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogErrorTests>(_logger);
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists");

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1}", 1);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1} {2}", 1, 2);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogErrorIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogError(_exception, "It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }
}
