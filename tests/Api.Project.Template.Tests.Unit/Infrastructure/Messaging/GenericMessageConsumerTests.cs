using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class GenericMessageConsumerTests
{
    private readonly ServiceCollection _services = new();
    private readonly Mock<IMessageBrokerAdapter> _mockAdapter = new();
    private readonly IConfiguration _configuration = CreateTestConfiguration();
    private readonly Mock<ILoggerAdapter<GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>>> _logger = new();
    
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        // Act
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        // Assert
        Assert.NotNull(consumer);
    }

    [Fact]
    public void Constructor_ImplementsIMessageConsumer()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        // Act
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        // Assert
        Assert.IsType<IMessageConsumer>(consumer, exactMatch: false);
    }

    [Fact]
    public async Task StartAsync_CallsAdapterConnectAsync()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        _mockAdapter.Setup(a => a.ConnectAsync(
            It.IsAny<MessageBrokerConfig>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAdapter.Setup(a => a.SubscribeAsync(
            It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await consumer.StartAsync(CancellationToken.None);

        // Assert
        _mockAdapter.Verify(a => a.ConnectAsync(
            It.IsAny<MessageBrokerConfig>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_CallsAdapterSubscribeAsync()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        _mockAdapter.Setup(a => a.ConnectAsync(
            It.IsAny<MessageBrokerConfig>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAdapter.Setup(a => a.SubscribeAsync(
            It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await consumer.StartAsync(CancellationToken.None);

        // Assert
        _mockAdapter.Verify(a => a.SubscribeAsync(
            It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_CallsAdapterDisconnectAsync()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        _mockAdapter.Setup(a => a.DisconnectAsync())
            .Returns(Task.CompletedTask);

        // Act
        await consumer.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        _mockAdapter.Verify(a => a.DisconnectAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CallsAdapterDisposeAsync()
    {
        // Arrange
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        _mockAdapter.Setup(a => a.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        // Act
        await consumer.DisposeAsync();

        // Assert
        _mockAdapter.Verify(a => a.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task MessageHandler_ResolvesProcessorFromScope()
    {
        // Arrange
        var mockProcessor = new Mock<IMessageProcessor<TestMessage>>();
        mockProcessor.Setup(p => p.ProcessAsync(
            It.IsAny<TestMessage>(),
            It.IsAny<MessageContext>()))
            .ReturnsAsync(MessageProcessingResult.Succeeded());

        var processorInstance = mockProcessor.Object;
        _services.AddScoped<IMessageProcessor<TestMessage>>(_ => processorInstance);
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        Func<TestMessage, MessageContext, Task<MessageProcessingResult>>? capturedHandler = null;

        _mockAdapter.Setup(a => a.ConnectAsync(
            It.IsAny<MessageBrokerConfig>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAdapter.Setup(a => a.SubscribeAsync(
            It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>, CancellationToken>(
                (handler, ct) => capturedHandler = handler)
            .Returns(Task.CompletedTask);

        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        await consumer.StartAsync(CancellationToken.None);

        Assert.NotNull(capturedHandler);

        var testMessage = new TestMessage { Id = 1, Name = "Test" };
        var testContext = new MessageContext
        {
            MessageId = "msg-1",
            CorrelationId = "corr-1"
        };

        // Act
        var result = await capturedHandler(testMessage, testContext);

        // Assert
        Assert.True(result.Success);
        mockProcessor.Verify(p => p.ProcessAsync(
            It.Is<TestMessage>(m => m.Id == 1 && m.Name == "Test"),
            It.IsAny<MessageContext>()), Times.Once);
    }

    [Fact]
    public async Task MessageHandler_HandlesProcessorException()
    {
        // Arrange
        var mockProcessor = new Mock<IMessageProcessor<TestMessage>>();
        mockProcessor.Setup(p => p.ProcessAsync(
            It.IsAny<TestMessage>(),
            It.IsAny<MessageContext>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var processorInstance = mockProcessor.Object;
        _services.AddScoped<IMessageProcessor<TestMessage>>(_ => processorInstance);
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        Func<TestMessage, MessageContext, Task<MessageProcessingResult>>? capturedHandler = null;

        _mockAdapter.Setup(a => a.ConnectAsync(
            It.IsAny<MessageBrokerConfig>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAdapter.Setup(a => a.SubscribeAsync(
            It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(),
            It.IsAny<CancellationToken>()))
            .Callback<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>, CancellationToken>(
                (handler, ct) => capturedHandler = handler)
            .Returns(Task.CompletedTask);

        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object,
            _configuration,
            scopeFactory,
            _logger.Object);

        await consumer.StartAsync(CancellationToken.None);

        Assert.NotNull(capturedHandler);

        var testMessage = new TestMessage { Id = 1, Name = "Test" };
        var testContext = new MessageContext { MessageId = "msg-1" };

        // Act
        var result = await capturedHandler(testMessage, testContext);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
        Assert.Equal("Test exception", result.ErrorReason);
    }

    // Helper methods

    private static IConfiguration CreateTestConfiguration()
    {
        var configData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:messaging"] = "amqp://localhost",
            ["MessageBus:Consumer:Queue"] = "test-queue",
            ["MessageBus:Consumer:Concurrency"] = "5",
            ["MessageBus:Consumer:MaxRetries"] = "3",
            ["MessageBus:Consumer:PrefetchCount"] = "10",
            ["MessageBus:RabbitMq:Exchange"] = "test-exchange",
            ["MessageBus:RabbitMq:RoutingKey"] = "test-key"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    // Test message type
    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
