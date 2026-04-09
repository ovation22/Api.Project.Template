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

var messagingProvider = builder.Configuration["MessagingProvider"] ?? "None";

if (messagingProvider == "RabbitMq")
{
    builder.AddRabbitMQClient(connectionName: "messaging");
    builder.Services.AddMessageBus(builder.Configuration);
    builder.Services.AddMessageConsumer<WeatherRequested, WeatherRequestProcessor, Worker>();
}
else if (messagingProvider == "ServiceBus")
{
    builder.Services.AddMessageBus(builder.Configuration);
    builder.Services.AddMessageConsumer<WeatherRequested, WeatherRequestProcessor, Worker>();
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
