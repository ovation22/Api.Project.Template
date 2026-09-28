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

    [Fact]
    public async Task StartAsync_PassesServiceBusTopicAndSubscriptionToAdapter()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost/",
                ["MessageBus:Consumer:Queue"] = "weather-requests",
                ["MessageBus:ServiceBus:Topic"] = "apiprojecttemplate.events",
                ["MessageBus:ServiceBus:SubscriptionName"] = "weather-sub"
            })
            .Build();
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        MessageBrokerConfig? captured = null;
        _mockAdapter.Setup(a => a.ConnectAsync(It.IsAny<MessageBrokerConfig>(), It.IsAny<CancellationToken>()))
            .Callback<MessageBrokerConfig, CancellationToken>((config, _) => captured = config)
            .Returns(Task.CompletedTask);
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object, configuration, scopeFactory, _logger.Object);

        // Act
        await consumer.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("apiprojecttemplate.events", captured.ProviderSpecific["Topic"]);
        Assert.Equal("weather-sub", captured.ProviderSpecific["SubscriptionName"]);
    }

    [Fact]
    public async Task MessageHandler_WhenCanceledBecauseConsumerIsStopping_RethrowsSoTheAdapterCanRequeue()
    {
        // Arrange
        var handler = await StartWithProcessorThatThrowsAsync(new OperationCanceledException());
        using var stopping = new CancellationTokenSource();
        await stopping.CancelAsync();
        var context = new MessageContext { MessageId = "msg-1", CancellationToken = stopping.Token };

        // Act
        var act = () => handler(new TestMessage { Id = 1 }, context);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task MessageHandler_WhenCanceledWhileNotStopping_ReturnsFailure()
    {
        // Arrange
        // e.g. an HTTP call inside the processor timing out — a genuine failure, not shutdown
        var handler = await StartWithProcessorThatThrowsAsync(new TaskCanceledException("timed out"));
        var context = new MessageContext { MessageId = "msg-1", CancellationToken = CancellationToken.None };

        // Act
        var result = await handler(new TestMessage { Id = 1 }, context);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task StartAsync_WithConsumerSection_UsesSectionValuesAndFallsBackToSharedSection()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://localhost",
                ["MessageBus:Consumer:Queue"] = "weather-requests",
                ["MessageBus:Consumer:Concurrency"] = "7",
                ["MessageBus:RabbitMq:RoutingKey"] = "WeatherRequested",
                ["MessageBus:Consumers:Audit:Queue"] = "audit-events",
                ["MessageBus:Consumers:Audit:RoutingKey"] = "Audit.*"
            })
            .Build();
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        MessageBrokerConfig? captured = null;
        _mockAdapter.Setup(a => a.ConnectAsync(It.IsAny<MessageBrokerConfig>(), It.IsAny<CancellationToken>()))
            .Callback<MessageBrokerConfig, CancellationToken>((config, _) => captured = config)
            .Returns(Task.CompletedTask);
        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object, configuration, scopeFactory, _logger.Object, "MessageBus:Consumers:Audit");

        // Act
        await consumer.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("audit-events", captured.Queue);                    // from the consumer's section
        Assert.Equal("Audit.*", captured.ProviderSpecific["RoutingKey"]); // per-consumer override
        Assert.Equal(7, captured.Concurrency);                           // falls back to MessageBus:Consumer
    }

    // Helper methods

    private async Task<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>> StartWithProcessorThatThrowsAsync(Exception exception)
    {
        var mockProcessor = new Mock<IMessageProcessor<TestMessage>>();
        mockProcessor.Setup(p => p.ProcessAsync(It.IsAny<TestMessage>(), It.IsAny<MessageContext>()))
            .ThrowsAsync(exception);
        _services.AddScoped<IMessageProcessor<TestMessage>>(_ => mockProcessor.Object);
        var scopeFactory = _services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        Func<TestMessage, MessageContext, Task<MessageProcessingResult>>? captured = null;
        _mockAdapter.Setup(a => a.SubscribeAsync(
                It.IsAny<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<TestMessage, MessageContext, Task<MessageProcessingResult>>, CancellationToken>((h, _) => captured = h)
            .Returns(Task.CompletedTask);

        var consumer = new GenericMessageConsumer<TestMessage, IMessageProcessor<TestMessage>>(
            _mockAdapter.Object, _configuration, scopeFactory, _logger.Object);
        await consumer.StartAsync(TestContext.Current.CancellationToken);

        return captured!;
    }

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
