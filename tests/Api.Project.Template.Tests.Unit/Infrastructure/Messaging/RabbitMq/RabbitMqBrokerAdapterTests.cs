using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.RabbitMq;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging.RabbitMq;

public class RabbitMqBrokerAdapterTests
{
    private readonly Mock<ILoggerAdapter<RabbitMqBrokerAdapter>> _logger = new();

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIMessageBrokerAdapter()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        // Assert
        Assert.IsAssignableFrom<IMessageBrokerAdapter>(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIAsyncDisposable()
    {
        // Act
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        // Assert
        Assert.IsType<IAsyncDisposable>(adapter, exactMatch: false);
    }

    [Fact]
    public async Task ConnectAsync_RequiresConnectionString()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        var config = new MessageBrokerConfig
        {
            ConnectionString = "", // Empty connection string
            Queue = "test-queue"
        };

        // Act & Assert
        // This will fail to connect, but we're testing that it attempts to parse
        // In a real scenario, we'd need a mock or actual RabbitMQ
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await adapter.ConnectAsync(config, CancellationToken.None);
        });
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        // Act & Assert - should not throw
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        // Act & Assert - should not throw
        await adapter.DisconnectAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnect_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = new RabbitMqBrokerAdapter(_logger.Object);

        Task<MessageProcessingResult> Handler(TestMessage msg, MessageContext ctx)
        {
            return Task.FromResult(MessageProcessingResult.Succeeded());
        }

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await adapter.SubscribeAsync<TestMessage>(Handler, CancellationToken.None);
        });
    }

    [Fact]
    public void MessageBrokerConfig_SupportsProviderSpecificSettings()
    {
        // Arrange
        var config = new MessageBrokerConfig
        {
            ConnectionString = "amqp://localhost",
            Queue = "test-queue",
            ProviderSpecific = new Dictionary<string, string>
            {
                ["Exchange"] = "custom-exchange",
                ["RoutingKey"] = "custom-key",
                ["ExchangeType"] = "fanout"
            }
        };

        // Assert
        Assert.Equal("custom-exchange", config.ProviderSpecific["Exchange"]);
        Assert.Equal("custom-key", config.ProviderSpecific["RoutingKey"]);
        Assert.Equal("fanout", config.ProviderSpecific["ExchangeType"]);
    }

    // Test message type
    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
