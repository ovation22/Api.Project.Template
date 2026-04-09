using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class RoutingMessagePublisherTests
{
    private readonly Mock<IMessagePublisher> _mockInnerPublisher;
    private readonly Mock<ILoggerAdapter<RoutingMessagePublisher>> _logger;

    public RoutingMessagePublisherTests()
    {
        _mockInnerPublisher = new Mock<IMessagePublisher>();
        _logger = new Mock<ILoggerAdapter<RoutingMessagePublisher>>();
    }

    [Fact]
    public async Task PublishAsync_WithConfiguredRoute_ResolvesDestinationFromConfig()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "test-queue",
                    RoutingKey = "TestMessage"
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "test-queue" &&
                o.Subject == "TestMessage"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithUnknownMessageType_FallsBackToDefaults()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            DefaultDestination = "default-exchange",
            DefaultRoutingKey = "default-key",
            Routes = new Dictionary<string, MessageRoute>() // Empty - no routes configured
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new UnknownMessage { Data = "test" };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "default-exchange" &&
                o.Subject == "default-key"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNoDefaultRoutingKey_UsesMessageTypeName()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            DefaultDestination = "default-exchange",
            DefaultRoutingKey = null, // No default routing key
            Routes = new Dictionary<string, MessageRoute>()
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new UnknownMessage { Data = "test" };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "default-exchange" &&
                o.Subject == "UnknownMessage"), // Uses type name as routing key
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithExplicitDestination_BypassesRoutingConfig()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "config-queue",
                    RoutingKey = "ConfigKey"
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };
        var explicitOptions = new MessagePublishOptions
        {
            Destination = "explicit-queue",
            Subject = "ExplicitKey"
        };

        // Act
        await publisher.PublishAsync(message, explicitOptions, TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "explicit-queue" &&
                o.Subject == "ExplicitKey"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithSameMessageTypeMultipleTimes_CachesRoutingResolution()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "test-queue",
                    RoutingKey = "TestMessage"
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message1 = new TestMessage { Id = Guid.NewGuid() };
        var message2 = new TestMessage { Id = Guid.NewGuid() };
        var message3 = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message1, cancellationToken: TestContext.Current.CancellationToken);
        await publisher.PublishAsync(message2, cancellationToken: TestContext.Current.CancellationToken);
        await publisher.PublishAsync(message3, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - inner publisher called 3 times with same routing
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            It.IsAny<TestMessage>(),
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "test-queue" &&
                o.Subject == "TestMessage"),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task PublishAsync_WithRouteSubjectOnly_UsesSubjectAsRoutingKey()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "test-queue",
                    Subject = "SubjectValue" // Subject, not RoutingKey
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "test-queue" &&
                o.Subject == "SubjectValue"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithRouteRoutingKeyPreferred_UsesRoutingKeyOverSubject()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "test-queue",
                    RoutingKey = "RoutingKeyValue",
                    Subject = "SubjectValue"
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert - RoutingKey takes precedence over Subject
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "test-queue" &&
                o.Subject == "RoutingKeyValue"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig();
        var publisher = CreatePublisher(routingConfig);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PublishAsync_WithRouteMetadata_MergesWithUserMetadata()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = "test-queue",
                    RoutingKey = "TestMessage",
                    Metadata = new Dictionary<string, string>
                    {
                        ["route-key"] = "route-value"
                    }
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Metadata != null &&
                o.Metadata.ContainsKey("route-key") &&
                (string)o.Metadata["route-key"] == "route-value"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithRouteDestinationNull_FallsBackToDefaultDestination()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig
        {
            DefaultDestination = "default-exchange",
            Routes = new Dictionary<string, MessageRoute>
            {
                ["TestMessage"] = new MessageRoute
                {
                    Destination = null, // No destination specified
                    RoutingKey = "TestMessage"
                }
            }
        };

        var publisher = CreatePublisher(routingConfig);
        var message = new TestMessage { Id = Guid.NewGuid() };

        // Act
        await publisher.PublishAsync(message, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _mockInnerPublisher.Verify(p => p.PublishAsync(
            message,
            It.Is<MessagePublishOptions>(o =>
                o.Destination == "default-exchange" &&
                o.Subject == "TestMessage"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WithAsyncDisposableInnerPublisher_DisposesInner()
    {
        // Arrange
        var mockAsyncDisposable = new Mock<IMessagePublisher>();
        mockAsyncDisposable.As<IAsyncDisposable>();

        var routingConfig = new MessageRoutingConfig();
        var publisher = new RoutingMessagePublisher(
            mockAsyncDisposable.Object,
            Options.Create(routingConfig),
            _logger.Object);

        // Act
        await publisher.DisposeAsync();

        // Assert
        mockAsyncDisposable.As<IAsyncDisposable>().Verify(d => d.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WithDisposableInnerPublisher_DisposesInner()
    {
        // Arrange
        var mockDisposable = new Mock<IMessagePublisher>();
        mockDisposable.As<IDisposable>();

        var routingConfig = new MessageRoutingConfig();
        var publisher = new RoutingMessagePublisher(
            mockDisposable.Object,
            Options.Create(routingConfig),
            _logger.Object);

        // Act
        await publisher.DisposeAsync();

        // Assert
        mockDisposable.As<IDisposable>().Verify(d => d.Dispose(), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullInnerPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoutingMessagePublisher(
            null!,
            Options.Create(routingConfig),
            _logger.Object));
    }

    [Fact]
    public void Constructor_WithNullRoutingConfig_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoutingMessagePublisher(
            _mockInnerPublisher.Object,
            null!,
            _logger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var routingConfig = new MessageRoutingConfig();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RoutingMessagePublisher(
            _mockInnerPublisher.Object,
            Options.Create(routingConfig),
            null!));
    }

    private RoutingMessagePublisher CreatePublisher(MessageRoutingConfig config)
    {
        return new RoutingMessagePublisher(
            _mockInnerPublisher.Object,
            Options.Create(config),
            (ILoggerAdapter<RoutingMessagePublisher>)_logger.Object);
    }

    // Test message types
    private record TestMessage
    {
        public Guid Id { get; init; }
    }

    private record UnknownMessage
    {
        public string Data { get; init; } = string.Empty;
    }
}
