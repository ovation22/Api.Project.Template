using System.Text;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.RabbitMq;
using RabbitMQ.Client;
using static Api.Project.Template.Tests.Integration.Messaging.TestSupport;

namespace Api.Project.Template.Tests.Integration.Messaging.RabbitMq;

[Trait("Category", "RabbitMq")]
public sealed class RabbitMqBrokerAdapterTests(RabbitMqFixture fixture) : IClassFixture<RabbitMqFixture>
{
    private const int MaxRetries = 3;

    [Fact]
    public async Task TransientFailure_IsRetriedUntilMaxRetriesThenDeadLettered()
    {
        // Arrange
        var queue = UniqueName("retry");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await PublishAsync(queue, Payload(RecordingHandler.TransientFailureId));
        var deadLettered = await EventuallyAsync(() => CountAsync($"{queue}.dlq"), count => count == 1);

        // Assert
        Assert.Equal([1, 2, 3], handler.DeliveryCountsFor(RecordingHandler.TransientFailureId));
        Assert.Equal(1u, deadLettered);
        Assert.Equal(0u, await CountAsync(queue));
    }

    [Fact]
    public async Task PermanentFailure_IsDeadLetteredWithoutRetry()
    {
        // Arrange
        var queue = UniqueName("permanent");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await PublishAsync(queue, Payload(RecordingHandler.PermanentFailureId));
        var deadLettered = await EventuallyAsync(() => CountAsync($"{queue}.dlq"), count => count == 1);

        // Assert
        Assert.Equal([1], handler.DeliveryCountsFor(RecordingHandler.PermanentFailureId));
        Assert.Equal(1u, deadLettered);
    }

    [Fact]
    public async Task MalformedMessage_IsDeadLettered()
    {
        // Arrange
        var queue = UniqueName("malformed");
        await using var adapter = await StartAdapterAsync(queue, new RecordingHandler());

        // Act
        await PublishAsync(queue, MalformedPayload);
        var deadLettered = await EventuallyAsync(() => CountAsync($"{queue}.dlq"), count => count == 1);

        // Assert
        Assert.Equal(1u, deadLettered);
    }

    [Fact]
    public async Task SuccessfulMessage_IsProcessedOnceAndNotDeadLettered()
    {
        // Arrange
        var queue = UniqueName("success");
        var handler = new RecordingHandler();
        await using var adapter = await StartAdapterAsync(queue, handler);

        // Act
        await PublishAsync(queue, Payload(RecordingHandler.SuccessId));
        var deliveries = await EventuallyAsync(
            () => Task.FromResult(handler.DeliveryCountsFor(RecordingHandler.SuccessId)), d => d.Count == 1);

        // Assert
        Assert.Equal([1], deliveries);
        Assert.Equal(0u, await CountAsync($"{queue}.dlq"));
        Assert.Equal(0u, await CountAsync(queue));
    }

    [Fact]
    public async Task StoppingMidMessage_ReturnsMessageToQueueInsteadOfDeadLettering()
    {
        // Arrange
        var queue = UniqueName("stop");
        var handler = new SlowHandler();
        using var stopping = new CancellationTokenSource();
        var adapter = new RabbitMqBrokerAdapter(Logger<RabbitMqBrokerAdapter>());
        await adapter.ConnectAsync(Config(queue), TestContext.Current.CancellationToken);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, stopping.Token);
        await PublishAsync(queue, Payload(1));
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Act
        await stopping.CancelAsync();
        // Let the handler react to cancellation before the channel closes. (Closing the channel returns
        // unacked messages on its own, which would mask what the adapter does with a cancelled message.)
        await Task.Delay(500, TestContext.Current.CancellationToken);
        await adapter.DisposeAsync();
        var queued = await EventuallyAsync(() => CountAsync(queue), count => count == 1);

        // Assert
        Assert.Equal((1u, 0u), (queued, await CountAsync($"{queue}.dlq")));
    }

    [Fact]
    public async Task ConnectAsync_WhenQueueExistsWithoutDeadLetterSettings_ThrowsClearError()
    {
        // Arrange
        var queue = UniqueName("legacy");
        await using (var connection = await fixture.CreateConnectionAsync())
        await using (var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
                cancellationToken: TestContext.Current.CancellationToken);
        }

        await using var adapter = new RabbitMqBrokerAdapter(Logger<RabbitMqBrokerAdapter>());

        // Act
        var act = () => adapter.ConnectAsync(Config(queue), TestContext.Current.CancellationToken);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Contains("without dead-letter settings", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        // Both the consumer and the DI container dispose the adapter singleton at shutdown.
        var adapter = await StartAdapterAsync(UniqueName("dispose"), new RecordingHandler());

        // Act
        await adapter.DisposeAsync();
        var secondDispose = await Record.ExceptionAsync(async () => await adapter.DisposeAsync());

        // Assert
        Assert.Null(secondDispose);
    }

    private MessageBrokerConfig Config(string queue) => new()
    {
        ConnectionString = fixture.ConnectionString,
        Queue = queue,
        MaxRetries = MaxRetries,
        Concurrency = 1,
        ProviderSpecific = { ["Exchange"] = $"{queue}.events", ["RoutingKey"] = "#" }
    };

    private async Task<RabbitMqBrokerAdapter> StartAdapterAsync(string queue, RecordingHandler handler)
    {
        var adapter = new RabbitMqBrokerAdapter(Logger<RabbitMqBrokerAdapter>());
        await adapter.ConnectAsync(Config(queue), TestContext.Current.CancellationToken);
        await adapter.SubscribeAsync<TestMessage>(handler.HandleAsync, TestContext.Current.CancellationToken);
        return adapter;
    }

    // Publishes straight to the queue through the default exchange.
    private async Task PublishAsync(string queue, string payload)
    {
        await using var connection = await fixture.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", queue, Encoding.UTF8.GetBytes(payload), TestContext.Current.CancellationToken);
    }

    private async Task<uint> CountAsync(string queue)
    {
        await using var connection = await fixture.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        return (await channel.QueueDeclarePassiveAsync(queue, TestContext.Current.CancellationToken)).MessageCount;
    }
}
