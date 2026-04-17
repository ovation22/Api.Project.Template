using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Logging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.Infrastructure.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging.RabbitMq;
using Api.Project.Template.Infrastructure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class MessageBusExtensionsTests
{
    [Theory]
    [InlineData("RabbitMq", "RabbitMq")]
    [InlineData("rabbitmq", "RabbitMq")]
    [InlineData("RABBITMQ", "RabbitMq")]
    [InlineData("ServiceBus", "ServiceBus")]
    [InlineData("servicebus", "ServiceBus")]
    [InlineData("SERVICEBUS", "ServiceBus")]
    public void ResolveProvider_WithExplicitProvider_ReturnsNormalizedValue(string input, string expected)
    {
        // Arrange
        var config = CreateEmptyConfiguration();

        // Act
        var result = MessageBusExtensions.ResolveProvider(input, config);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveProvider_WithInvalidProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateEmptyConfiguration();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MessageBusExtensions.ResolveProvider("Redis", config));

        Assert.Contains("Invalid MessageBus:Routing:Provider value: 'Redis'", ex.Message);
        Assert.Contains("Valid values: 'RabbitMq', 'ServiceBus', 'Sqs', 'Auto'", ex.Message);
    }

    [Theory]
    [InlineData("Auto")]
    [InlineData("auto")]
    [InlineData("")]
    [InlineData(null)]
    public void ResolveProvider_WithAutoOrEmpty_DetectsFromConnectionStrings(string? providerValue)
    {
        // Arrange - only RabbitMQ connection string
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/"
            })
            .Build();

        // Act
        var result = MessageBusExtensions.ResolveProvider(providerValue, config);

        // Assert
        Assert.Equal("RabbitMq", result);
    }

    [Fact]
    public void DetectProvider_WithOnlyRabbitMqConnectionString_ReturnsRabbitMq()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/"
            })
            .Build();

        // Act
        var result = MessageBusExtensions.DetectProvider(config);

        // Assert
        Assert.Equal("RabbitMq", result);
    }

    [Fact]
    public void DetectProvider_WithOnlyServiceBusConnectionString_ReturnsServiceBus()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();

        // Act
        var result = MessageBusExtensions.DetectProvider(config);

        // Assert
        Assert.Equal("ServiceBus", result);
    }

    [Fact]
    public void DetectProvider_WithBothConnectionStrings_PrefersRabbitMq()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/",
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test"
            })
            .Build();

        // Act
        var result = MessageBusExtensions.DetectProvider(config);

        // Assert
        Assert.Equal("RabbitMq", result);
    }

    [Fact]
    public void DetectProvider_WithNoConnectionStrings_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateEmptyConfiguration();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MessageBusExtensions.DetectProvider(config));

        Assert.Contains("No message broker connection string found", ex.Message);
        Assert.Contains("ConnectionStrings:messaging", ex.Message);
        Assert.Contains("ConnectionStrings:servicebus", ex.Message);
    }

    [Theory]
    [InlineData("MessageBus:RabbitMq:ConnectionString", "amqp://localhost")]
    [InlineData("MessageBus:RabbitMq", "amqp://localhost")]
    [InlineData("MessageBus__RabbitMq__ConnectionString", "amqp://localhost")]
    [InlineData("MessageBus__RabbitMq", "amqp://localhost")]
    [InlineData("ConnectionStrings:RabbitMq", "amqp://localhost")]
    [InlineData("ConnectionStrings:messaging", "amqp://localhost")]
    public void HasRabbitMqConnectionString_WithValidConnectionString_ReturnsTrue(string key, string value)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = value
            })
            .Build();

        // Act
        var result = MessageBusExtensions.HasRabbitMqConnectionString(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasRabbitMqConnectionString_WithNoConnectionString_ReturnsFalse()
    {
        // Arrange
        var config = CreateEmptyConfiguration();

        // Act
        var result = MessageBusExtensions.HasRabbitMqConnectionString(config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasRabbitMqConnectionString_WithEmptyConnectionString_ReturnsFalse()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = ""
            })
            .Build();

        // Act
        var result = MessageBusExtensions.HasRabbitMqConnectionString(config);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("ConnectionStrings:servicebus", "Endpoint=sb://localhost")]
    public void HasServiceBusConnectionString_WithValidConnectionString_ReturnsTrue(string key, string value)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [key] = value
            })
            .Build();

        // Act
        var result = MessageBusExtensions.HasServiceBusConnectionString(config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasServiceBusConnectionString_WithNoConnectionString_ReturnsFalse()
    {
        // Arrange
        var config = CreateEmptyConfiguration();

        // Act
        var result = MessageBusExtensions.HasServiceBusConnectionString(config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasServiceBusConnectionString_WithEmptyConnectionString_ReturnsFalse()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = ""
            })
            .Build();

        // Act
        var result = MessageBusExtensions.HasServiceBusConnectionString(config);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AddMessageBus_WithRabbitMqProvider_RegistersRabbitMqBrokerAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/",
                ["MessageBus:Routing:Provider"] = "RabbitMq"
            })
            .Build();

        // Act
        services.AddMessageBus(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var adapter = serviceProvider.GetService<IMessageBrokerAdapter>();
        Assert.NotNull(adapter);
        Assert.IsType<RabbitMqBrokerAdapter>(adapter);
    }

    [Fact]
    public void AddMessageBus_WithServiceBusProvider_RegistersServiceBusBrokerAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:servicebus"] = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                ["MessageBus:Routing:Provider"] = "ServiceBus"
            })
            .Build();

        // Act
        services.AddMessageBus(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var adapter = serviceProvider.GetService<IMessageBrokerAdapter>();
        Assert.NotNull(adapter);
        Assert.IsType<ServiceBusBrokerAdapter>(adapter);
    }

    [Fact]
    public void AddMessageBus_WithAutoProvider_RabbitMqConnectionString_RegistersRabbitMqBrokerAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/",
                ["MessageBus:Routing:Provider"] = "Auto"
            })
            .Build();

        // Act
        services.AddMessageBus(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var adapter = serviceProvider.GetService<IMessageBrokerAdapter>();
        Assert.NotNull(adapter);
        Assert.IsType<RabbitMqBrokerAdapter>(adapter);
    }

    [Fact]
    public void AddMessageBus_RegistersBothPublisherAndConsumerAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/",
                ["MessageBus:Routing:Provider"] = "RabbitMq"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);

        // Act
        services.AddMessageBus(config);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var publisher = serviceProvider.GetService<IMessagePublisher>();
        var adapter = serviceProvider.GetService<IMessageBrokerAdapter>();

        Assert.NotNull(publisher);
        Assert.NotNull(adapter);
        Assert.IsType<RoutingMessagePublisher>(publisher);
        Assert.IsType<RabbitMqBrokerAdapter>(adapter);
    }

    [Fact]
    public void AddMessageBus_WithInvalidProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:messaging"] = "amqp://guest:guest@localhost:5672/",
                ["MessageBus:Routing:Provider"] = "Kafka"
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBus(config));

        Assert.Contains("Invalid MessageBus:Routing:Provider value: 'Kafka'", ex.Message);
        Assert.Contains("Valid values: 'RabbitMq', 'ServiceBus', 'Sqs', 'Auto'", ex.Message);
    }

    private static IConfiguration CreateEmptyConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
    }
}
