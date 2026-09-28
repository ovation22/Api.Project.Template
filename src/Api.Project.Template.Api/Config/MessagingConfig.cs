using Api.Project.Template.Application.Messaging.Abstractions;
using Api.Project.Template.Infrastructure.Messaging;

namespace Api.Project.Template.Api.Config;

public static class MessagingConfig
{
    /// <summary>
    /// Registers the message publisher for the configured <c>MessagingProvider</c>:
    /// "None" (or unset) registers a no-op publisher; "RabbitMq", "ServiceBus" or "Sqs" registers that broker
    /// (the value is case-insensitive). Any other value fails at startup rather than silently disabling messaging.
    /// </summary>
    public static void AddMessaging(this IHostApplicationBuilder builder)
    {
        var messagingProvider = builder.Configuration["MessagingProvider"];

        switch (messagingProvider?.ToLowerInvariant())
        {
            case null or "" or "none":
                builder.Services.AddSingleton<IMessagePublisher, NullMessagePublisher>();
                break;

            // AddMessageBus resolves the same provider from MessagingProvider
            case "rabbitmq" or "servicebus" or "sqs":
                builder.Services.AddMessageBus(builder.Configuration);
                break;

            default:
                throw new InvalidOperationException(
                    $"Invalid MessagingProvider value: '{messagingProvider}'. Valid values: 'None', 'RabbitMq', 'ServiceBus', 'Sqs'.");
        }
    }
}
