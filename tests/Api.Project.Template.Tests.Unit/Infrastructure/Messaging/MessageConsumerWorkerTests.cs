using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "MessageConsumerWorker")]
public class MessageConsumerWorkerTests
{
    private readonly Mock<IMessageConsumer> _consumer = new();
    private readonly Mock<ILoggerAdapter<MessageConsumerWorker<TestMessage, IMessageProcessor<TestMessage>>>> _logger = new();
    private readonly TaskCompletionSource _consumerStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MessageConsumerWorkerTests()
    {
        _consumer.Setup(c => c.StartAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _consumerStarted.TrySetResult())
            .Returns(Task.CompletedTask);
    }

    // BackgroundService.StartAsync returns before ExecuteAsync runs; stopping immediately would cancel it
    // before it starts, so wait until the worker has actually started the consumer.
    private async Task StartWorkerAsync(MessageConsumerWorker<TestMessage, IMessageProcessor<TestMessage>> worker)
    {
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await _consumerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task StartAsync_StartsConsumer()
    {
        // Arrange
        using var worker = new MessageConsumerWorker<TestMessage, IMessageProcessor<TestMessage>>(_consumer.Object, _logger.Object);

        // Act
        await StartWorkerAsync(worker);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _consumer.Verify(c => c.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_StopsConsumerButDoesNotDisposeIt()
    {
        // Arrange
        // The consumer is a DI singleton — the container owns and disposes it. Disposing it here
        // as well caused a double dispose (ObjectDisposedException on SQS Worker shutdown).
        using var worker = new MessageConsumerWorker<TestMessage, IMessageProcessor<TestMessage>>(_consumer.Object, _logger.Object);
        await StartWorkerAsync(worker);

        // Act
        await worker.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _consumer.Verify(c => c.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
        _consumer.Verify(c => c.Dispose(), Times.Never);
        _consumer.Verify(c => c.DisposeAsync(), Times.Never);
    }

    public class TestMessage;
}
