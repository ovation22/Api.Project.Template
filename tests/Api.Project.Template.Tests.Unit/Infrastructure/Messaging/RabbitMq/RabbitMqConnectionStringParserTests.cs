using Api.Project.Template.Infrastructure.Messaging.RabbitMq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Messaging.RabbitMq;

[Trait("Category", "RabbitMqConnectionStringParser")]
public class RabbitMqConnectionStringParserTests
{
    [Fact]
    public void Parse_AmqpUri_SetsHostPortCredentialsAndVirtualHost()
    {
        // Arrange
        const string connectionString = "amqp://user:secret@broker.local:5673/orders";

        // Act
        var factory = RabbitMqConnectionStringParser.Parse(connectionString);

        // Assert
        Assert.Equal("broker.local", factory.HostName);
        Assert.Equal(5673, factory.Port);
        Assert.Equal("user", factory.UserName);
        Assert.Equal("secret", factory.Password);
        Assert.Equal("orders", factory.VirtualHost);
    }

    [Fact]
    public void Parse_KeyValueFormat_SetsHostPortCredentialsAndVirtualHost()
    {
        // Arrange
        const string connectionString = "Host=broker.local;Port=5673;Username=user;Password=secret;VirtualHost=orders";

        // Act
        var factory = RabbitMqConnectionStringParser.Parse(connectionString);

        // Assert
        Assert.Equal("broker.local", factory.HostName);
        Assert.Equal(5673, factory.Port);
        Assert.Equal("user", factory.UserName);
        Assert.Equal("secret", factory.Password);
        Assert.Equal("orders", factory.VirtualHost);
    }

    [Fact]
    public void Parse_KeyValueFormat_KeysAreCaseInsensitiveAndAliasesWork()
    {
        // Arrange
        const string connectionString = "hostname=broker.local; user=user; pwd=secret; vhost=orders";

        // Act
        var factory = RabbitMqConnectionStringParser.Parse(connectionString);

        // Assert
        Assert.Equal("broker.local", factory.HostName);
        Assert.Equal("user", factory.UserName);
        Assert.Equal("secret", factory.Password);
        Assert.Equal("orders", factory.VirtualHost);
    }
}
