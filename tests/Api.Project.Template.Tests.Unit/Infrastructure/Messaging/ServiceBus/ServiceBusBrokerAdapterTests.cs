using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging.ServiceBus;

public class ServiceBusBrokerAdapterTests
{
    private readonly Mock<ILoggerAdapter<ServiceBusBrokerAdapter>> _logger = new();

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIMessageBrokerAdapter()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);

        // Assert
        Assert.IsAssignableFrom<IMessageBrokerAdapter>(adapter);
    }

    [Fact]
    public void Constructor_ImplementsIAsyncDisposable()
    {
        // Act
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);
        // Assert
        Assert.IsAssignableFrom<IAsyncDisposable>(adapter);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);

        // Act & Assert - should not throw
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisconnectAsync_CanBeCalledWithoutConnect()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);

        // Act & Assert - should not throw
        await adapter.DisconnectAsync();
    }

    [Fact]
    public async Task SubscribeAsync_WithoutConnect_ThrowsInvalidOperationException()
    {
        // Arrange
        var adapter = new ServiceBusBrokerAdapter(_logger.Object);

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
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
            Queue = "test-queue",
            ProviderSpecific = new Dictionary<string, string>
            {
                ["SubscriptionName"] = "test-subscription"
            }
        };

        // Assert
        Assert.Equal("test-subscription", config.ProviderSpecific["SubscriptionName"]);
    }

    [Fact]
    public async Task ConnectAsync_WithoutTopic_ProcessesQueue()
    {
        // Arrange
        await using var adapter = new ServiceBusBrokerAdapter(_logger.Object);
        var config = new MessageBrokerConfig { ConnectionString = TestConnectionString, Queue = "weather-requests" };

        // Act
        // ConnectAsync only configures the processor; nothing connects until SubscribeAsync.
        await adapter.ConnectAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("weather-requests", adapter.EntityPath);
    }

    [Fact]
    public async Task ConnectAsync_WithTopic_ProcessesTopicSubscription()
    {
        // Arrange
        await using var adapter = new ServiceBusBrokerAdapter(_logger.Object);
        var config = new MessageBrokerConfig
        {
            ConnectionString = TestConnectionString,
            Queue = "weather-requests",
            ProviderSpecific = { ["Topic"] = "apiprojecttemplate.events", ["SubscriptionName"] = "weather-sub" }
        };

        // Act
        await adapter.ConnectAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("apiprojecttemplate.events/Subscriptions/weather-sub", adapter.EntityPath, ignoreCase: true);
    }

    [Fact]
    public async Task ConnectAsync_WithTopicAndNoSubscriptionName_UsesQueueAsSubscription()
    {
        // Arrange
        await using var adapter = new ServiceBusBrokerAdapter(_logger.Object);
        var config = new MessageBrokerConfig
        {
            ConnectionString = TestConnectionString,
            Queue = "weather-requests",
            ProviderSpecific = { ["Topic"] = "apiprojecttemplate.events" }
        };

        // Act
        await adapter.ConnectAsync(config, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("apiprojecttemplate.events/Subscriptions/weather-requests", adapter.EntityPath, ignoreCase: true);
    }

    private const string TestConnectionString =
        "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test";

    // Test message type
    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
