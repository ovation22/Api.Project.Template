using Api.Project.Template.Tests.Integration.Messaging.RabbitMq;
using Api.Project.Template.Tests.Integration.Messaging.Sqs;

namespace Api.Project.Template.Tests.Integration.Messaging;

// One container per broker for the whole run: test classes in a collection share its fixture.
// Fewer container starts also means fewer chances for Docker API hiccups on Windows.

[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqFixture>
{
    public const string Name = "RabbitMQ";
}

[CollectionDefinition(Name)]
public sealed class LocalStackCollection : ICollectionFixture<LocalStackFixture>
{
    public const string Name = "LocalStack";
}
