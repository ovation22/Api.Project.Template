using Api.Project.Template.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging;

public class MessageRoutingConfigTests
{
    [Fact]
    public void Bind_WithFullConfiguration_BindsAllProperties()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Provider"] = "RabbitMq",
                ["MessageBus:Routing:DefaultDestination"] = "apiprojecttemplate.events",
                ["MessageBus:Routing:DefaultRoutingKey"] = "default",
                ["MessageBus:Routing:Routes:WeatherRequested:Destination"] = "sample-requests",
                ["MessageBus:Routing:Routes:WeatherRequested:RoutingKey"] = "WeatherRequested",
                ["MessageBus:Routing:Routes:WeatherRequested:Subject"] = "sample-subject"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("RabbitMq", routingConfig.Provider);
        Assert.Equal("apiprojecttemplate.events", routingConfig.DefaultDestination);
        Assert.Equal("default", routingConfig.DefaultRoutingKey);
        Assert.Single(routingConfig.Routes);
        Assert.True(routingConfig.Routes.ContainsKey("WeatherRequested"));

        var weatherRoute = routingConfig.Routes["WeatherRequested"];
        Assert.Equal("sample-requests", weatherRoute.Destination);
        Assert.Equal("WeatherRequested", weatherRoute.RoutingKey);
        Assert.Equal("sample-subject", weatherRoute.Subject);
    }

    [Fact]
    public void Bind_WithEmptyConfiguration_ProducesValidDefaults()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>()
            ?? new MessageRoutingConfig();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("Auto", routingConfig.Provider);
        Assert.Null(routingConfig.DefaultDestination);
        Assert.Null(routingConfig.DefaultRoutingKey);
        Assert.Empty(routingConfig.Routes);
    }

    [Fact]
    public void Bind_WithMetadata_BindsMetadataDictionary()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Routes:WeatherRequested:Destination"] = "sample-requests",
                ["MessageBus:Routing:Routes:WeatherRequested:Metadata:priority"] = "high",
                ["MessageBus:Routing:Routes:WeatherRequested:Metadata:retryCount"] = "3"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Single(routingConfig.Routes);

        var weatherRoute = routingConfig.Routes["WeatherRequested"];
        Assert.NotNull(weatherRoute.Metadata);
        Assert.Equal(2, weatherRoute.Metadata.Count);
        Assert.Equal("high", weatherRoute.Metadata["priority"]);
        Assert.Equal("3", weatherRoute.Metadata["retryCount"]);
    }

    [Fact]
    public void Bind_WithProviderOnly_UsesDefaultsForOtherProperties()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MessageBus:Routing:Provider"] = "ServiceBus"
            })
            .Build();

        // Act
        var routingConfig = config.GetSection("MessageBus:Routing").Get<MessageRoutingConfig>();

        // Assert
        Assert.NotNull(routingConfig);
        Assert.Equal("ServiceBus", routingConfig.Provider);
        Assert.Null(routingConfig.DefaultDestination);
        Assert.Null(routingConfig.DefaultRoutingKey);
        Assert.Empty(routingConfig.Routes);
    }

    [Fact]
    public void MessageRoute_DefaultValues_AreNull()
    {
        // Arrange & Act
        var route = new MessageRoute();

        // Assert
        Assert.Null(route.Destination);
        Assert.Null(route.RoutingKey);
        Assert.Null(route.Subject);
        Assert.Null(route.Metadata);
    }

    [Fact]
    public void MessageRoutingConfig_DefaultProvider_IsAuto()
    {
        // Arrange & Act
        var config = new MessageRoutingConfig();

        // Assert
        Assert.Equal("Auto", config.Provider);
    }

    [Fact]
    public void MessageRoutingConfig_DefaultRoutes_IsEmptyDictionary()
    {
        // Arrange & Act
        var config = new MessageRoutingConfig();

        // Assert
        Assert.NotNull(config.Routes);
        Assert.Empty(config.Routes);
    }
}
