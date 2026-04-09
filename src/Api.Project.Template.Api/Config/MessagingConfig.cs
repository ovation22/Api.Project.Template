using Api.Project.Template.Infrastructure.Messaging;

namespace Api.Project.Template.Api.Config;

public static class MessagingConfig
{
    public static void AddRabbitMqMessageBus(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMessageBus(builder.Configuration);
    }

    public static void AddServiceBusMessageBus(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMessageBus(builder.Configuration);
    }
}
