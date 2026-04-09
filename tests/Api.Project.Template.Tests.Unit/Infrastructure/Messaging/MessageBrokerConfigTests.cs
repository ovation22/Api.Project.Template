
using Api.Project.Template.Infrastructure.Messaging;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class MessageBrokerConfigTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var config = new MessageBrokerConfig();

        // Assert
        Assert.Equal(string.Empty, config.ConnectionString);
        Assert.Equal(string.Empty, config.Queue);
        Assert.Equal(5, config.Concurrency);
        Assert.Equal(3, config.MaxRetries);
        Assert.Equal(10, config.PrefetchCount);
        Assert.NotNull(config.ProviderSpecific);
        Assert.Empty(config.ProviderSpecific);
    }

    [Fact]
    public void ConnectionString_CanBeSet()
    {
        // Arrange
        var config = new MessageBrokerConfig();
        var connectionString = "amqp://localhost:5672";

        // Act
        config.ConnectionString = connectionString;

        // Assert
        Assert.Equal(connectionString, config.ConnectionString);
    }

    [Fact]
    public void Queue_CanBeSet()
    {
        // Arrange
        var config = new MessageBrokerConfig();
        var queue = "my-queue";

        // Act
        config.Queue = queue;

        // Assert
        Assert.Equal(queue, config.Queue);
    }

    [Fact]
    public void Concurrency_CanBeSet()
    {
        // Arrange
        var config = new MessageBrokerConfig();

        // Act
        config.Concurrency = 10;

        // Assert
        Assert.Equal(10, config.Concurrency);
    }

    [Fact]
    public void MaxRetries_CanBeSet()
    {
        // Arrange
        var config = new MessageBrokerConfig();

        // Act
        config.MaxRetries = 5;

        // Assert
        Assert.Equal(5, config.MaxRetries);
    }

    [Fact]
    public void PrefetchCount_CanBeSet()
    {
        // Arrange
        var config = new MessageBrokerConfig();

        // Act
        config.PrefetchCount = 20;

        // Assert
        Assert.Equal(20, config.PrefetchCount);
    }

    [Fact]
    public void ProviderSpecific_CanAddKeyValuePairs()
    {
        // Arrange
        var config = new MessageBrokerConfig();

        // Act
        config.ProviderSpecific["Exchange"] = "my-exchange";
        config.ProviderSpecific["RoutingKey"] = "my-routing-key";

        // Assert
        Assert.Equal(2, config.ProviderSpecific.Count);
        Assert.Equal("my-exchange", config.ProviderSpecific["Exchange"]);
        Assert.Equal("my-routing-key", config.ProviderSpecific["RoutingKey"]);
    }

    [Fact]
    public void ProviderSpecific_CanBeReplacedWithNewDictionary()
    {
        // Arrange
        var config = new MessageBrokerConfig();
        var customSettings = new Dictionary<string, string>
        {
            ["SessionEnabled"] = "true",
            ["PrefetchCount"] = "15"
        };

        // Act
        config.ProviderSpecific = customSettings;

        // Assert
        Assert.Equal(2, config.ProviderSpecific.Count);
        Assert.Equal("true", config.ProviderSpecific["SessionEnabled"]);
        Assert.Equal("15", config.ProviderSpecific["PrefetchCount"]);
    }

    [Fact]
    public void DefaultConcurrency_IsFive()
    {
        // Act
        var config = new MessageBrokerConfig();

        // Assert
        Assert.Equal(5, config.Concurrency);
    }

    [Fact]
    public void DefaultMaxRetries_IsThree()
    {
        // Act
        var config = new MessageBrokerConfig();

        // Assert
        Assert.Equal(3, config.MaxRetries);
    }

    [Fact]
    public void DefaultPrefetchCount_IsTen()
    {
        // Act
        var config = new MessageBrokerConfig();

        // Assert
        Assert.Equal(10, config.PrefetchCount);
    }
}
