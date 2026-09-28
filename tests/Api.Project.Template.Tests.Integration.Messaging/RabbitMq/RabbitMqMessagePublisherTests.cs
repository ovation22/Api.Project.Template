using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using static Api.Project.Template.Tests.Integration.Messaging.TestSupport;

namespace Api.Project.Template.Tests.Integration.Messaging.RabbitMq;

[Trait("Category", "RabbitMq")]
[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqMessagePublisherTests(RabbitMqFixture fixture)
{
    [Fact]
    public async Task Publish_ToExchangeOtherThanTheDefault_DeclaresItAndDelivers()
    {
        // Arrange
        // A route can point at any exchange; the publisher used to declare only its configured default,
        // so the broker closed the channel (404) and every publish failed after its retries.
        var exchange = UniqueName("orders") + ".events";
        var queue = UniqueName("orders-queue");
        await using var publisher = CreatePublisher();
        var options = new MessagePublishOptions { Destination = exchange, Subject = "OrderPlaced" };

        // Act
        // First publish declares the exchange; then bind a queue and publish again to check delivery.
        await publisher.PublishAsync(new TestMessage(1), options, TestContext.Current.CancellationToken);
        await BindQueueAsync(queue, exchange, "OrderPlaced");
        await publisher.PublishAsync(new TestMessage(2), options, TestContext.Current.CancellationToken);
        var delivered = await EventuallyAsync(() => CountAsync(queue), count => count == 1);

        // Assert
        Assert.Equal(1u, delivered);
    }

    private RabbitMqMessagePublisher CreatePublisher()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:messaging"] = fixture.ConnectionString })
            .Build();
        return new RabbitMqMessagePublisher(configuration, Logger<RabbitMqMessagePublisher>());
    }

    private async Task BindQueueAsync(string queue, string exchange, string routingKey)
    {
        await using var connection = await fixture.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        // RabbitMQ 4.3 rejects transient non-exclusive queues, so declare it durable
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueBindAsync(queue, exchange, routingKey, cancellationToken: TestContext.Current.CancellationToken);
    }

    private async Task<uint> CountAsync(string queue)
    {
        await using var connection = await fixture.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        return (await channel.QueueDeclarePassiveAsync(queue, TestContext.Current.CancellationToken)).MessageCount;
    }
}
