using Api.Project.Template.Application.Messaging;
using Api.Project.Template.Application.Messaging.Abstractions;

namespace Api.Project.Template.Infrastructure.Messaging;

/// <summary>
/// No-op IMessagePublisher used when MessagingProvider is "None".
/// Lets features that publish events run without a broker.
/// </summary>
public sealed class NullMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<T>(T message, MessagePublishOptions? options = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
