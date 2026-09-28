using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Infrastructure.Logging;
using Api.Project.Template.Infrastructure.Messaging;
using Api.Project.Template.ServiceDefaults;
using Api.Project.Template.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddSingleton(typeof(ILoggerAdapter<>), typeof(LoggerAdapter<>));

// "None" (or unset) runs no consumers. The broker is resolved from MessagingProvider by AddMessageBus.
var messagingProvider = builder.Configuration["MessagingProvider"];

switch (messagingProvider?.ToLowerInvariant())
{
    case null or "" or "none":
        break;

    case "rabbitmq" or "servicebus" or "sqs":
        builder.Services.AddMessageBus(builder.Configuration);
        builder.Services.AddMessageConsumer<WeatherRequested, WeatherRequestProcessor, Worker>();
        break;

    default:
        throw new InvalidOperationException(
            $"Invalid MessagingProvider value: '{messagingProvider}'. Valid values: 'None', 'RabbitMq', 'ServiceBus', 'Sqs'.");
}

try
{
    Log.Information("Starting Api.Project.Template.Worker");
    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
