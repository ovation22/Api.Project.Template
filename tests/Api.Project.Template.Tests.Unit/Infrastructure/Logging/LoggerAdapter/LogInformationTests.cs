using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Api.Project.Template.Infrastructure.Logging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Logging.LoggerAdapter;

public class LogInformationTests
{
    private readonly Exception _exception;
    private readonly ILogger<LogInformationTests> _logger;
    private readonly LoggerAdapter<LogInformationTests> _loggerAdapter;

    public LogInformationTests()
    {
        _exception = new Exception(); 
        _logger = Substitute.For<ILogger<LogInformationTests>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        _loggerAdapter = new LoggerAdapter<LogInformationTests>(_logger);
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithMessage_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists");

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithOneArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1}", 1);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithTwoArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1} {2}", 1, 2);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }

    [Fact]
    public void GivenLogInformationIsEnabled_WhenCalledWithThreeArg_ThenLogs()
    {
        // Arrange
        // Act
        _loggerAdapter.LogInformation("It exists {1} {2} {3}", 1, 2, 3);

        // Assert
        Assert.Equal(2, _logger.ReceivedCalls().Count());
    }
}
