using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Features.Weather.Events;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Features.Weather.Events;

[Trait("Category", "WeatherForecastRequestedEventHandler")]
public class WeatherForecastRequestedEventHandlerTests
{
    private readonly Mock<IMessagePublisher> _publisher = new();
    private readonly Mock<ILoggerAdapter<WeatherForecastRequestedEventHandler>> _logger = new();
    private readonly WeatherForecastRequestedEvent _event = new(1, 10, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_PublishesTheEvent()
    {
        // Arrange
        var handler = new WeatherForecastRequestedEventHandler(_publisher.Object, _logger.Object);

        // Act
        await handler.Handle(_event, TestContext.Current.CancellationToken);

        // Assert
        _publisher.Verify(p => p.PublishAsync(_event, It.IsAny<MessagePublishOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPublishFails_LogsAndDoesNotThrow()
    {
        // Arrange
        // A broker outage must not fail the request that raised the event.
        var failure = new InvalidOperationException("broker unreachable");
        _publisher.Setup(p => p.PublishAsync(_event, It.IsAny<MessagePublishOptions?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var handler = new WeatherForecastRequestedEventHandler(_publisher.Object, _logger.Object);

        // Act
        var exception = await Record.ExceptionAsync(() => handler.Handle(_event, TestContext.Current.CancellationToken));

        // Assert
        Assert.Null(exception);
        _logger.Verify(l => l.LogWarning(failure, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestIsCanceled_Rethrows()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _publisher.Setup(p => p.PublishAsync(_event, It.IsAny<MessagePublishOptions?>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));
        var handler = new WeatherForecastRequestedEventHandler(_publisher.Object, _logger.Object);

        // Act
        var act = () => handler.Handle(_event, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }
}
