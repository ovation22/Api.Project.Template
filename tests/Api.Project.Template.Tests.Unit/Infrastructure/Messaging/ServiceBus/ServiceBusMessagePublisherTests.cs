using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging.ServiceBus;

public class ServiceBusMessagePublisherTests
{
    private readonly Mock<ILoggerAdapter<ServiceBusMessagePublisher>> _logger = new();

    [Fact]
    public void Constructor_WithValidConfiguration_Succeeds()
    {
        // Arrange
        var config = CreateTestConfiguration();

        // Act
        var publisher = new ServiceBusMessagePublisher(config, _logger.Object);

        // Assert
        Assert.NotNull(publisher);
    }

    [Fact]
    public void Constructor_WithMissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new ServiceBusMessagePublisher(config, _logger.Object));
    }

    [Fact]
    public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var config = CreateTestConfiguration();
        var publisher = new ServiceBusMessagePublisher(config, _logger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestMessage>(null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    private static IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();
    }

    private record TestMessage
    {
        public Guid Id { get; init; }
        public string Content { get; init; } = string.Empty;
    }
}
