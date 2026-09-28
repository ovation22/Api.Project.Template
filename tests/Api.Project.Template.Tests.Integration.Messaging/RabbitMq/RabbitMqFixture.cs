using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Api.Project.Template.Tests.Integration.Messaging.RabbitMq;

/// <summary>
/// One RabbitMQ container shared by the RabbitMQ tests; each test uses its own uniquely named queue.
/// </summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    public string ConnectionString => _container!.GetConnectionString();

    // Same major/minor version the AppHost runs
    public async ValueTask InitializeAsync()
        => _container = await TestSupport.StartContainerAsync(() => new RabbitMqBuilder("rabbitmq:4.3").Build());

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
            await TestSupport.StopContainerAsync(_container);
    }

    public async Task<IConnection> CreateConnectionAsync()
        => await new ConnectionFactory { Uri = new Uri(ConnectionString) }.CreateConnectionAsync();
}
