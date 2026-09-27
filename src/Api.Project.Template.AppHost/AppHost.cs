var builder = DistributedApplication.CreateBuilder(args);

var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var messagingProvider = builder.Configuration["MessagingProvider"] ?? "None";

var api = builder.AddProject<Projects.Api_Project_Template_Api>("api-project-template-api")
    .WithEnvironment("DatabaseProvider", provider)
    .WithEnvironment("MessagingProvider", messagingProvider);

var worker = builder.AddProject<Projects.Api_Project_Template_Worker>("api-project-template-worker")
    .WithEnvironment("MessagingProvider", messagingProvider);

if (provider == "PostgreSQL")
{
    var pg = builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("ApiProjectTemplate");

    api.WithReference(pg).WaitFor(pg);
}
else
{
    var sql = builder.AddSqlServer("sql", port: 59944)
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("ApiProjectTemplate");

    api.WithReference(sql).WaitFor(sql);
}

if (messagingProvider == "RabbitMq")
{
    var rabbit = builder.AddRabbitMQ("messaging")
        .WithDataVolume(isReadOnly: false)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithManagementPlugin();

    api.WithReference(rabbit).WaitFor(rabbit);
    worker.WithReference(rabbit).WaitFor(rabbit);
}
else if (messagingProvider == "ServiceBus")
{
    // The emulator is used in run mode only; publish mode provisions a real namespace.
    // The topic mirrors the RabbitMQ exchange so routing config works for both providers.
    var serviceBus = builder.AddAzureServiceBus("servicebus")
        .RunAsEmulator();

    serviceBus.AddServiceBusTopic("events", topicName: "apiprojecttemplate.events")
        .AddServiceBusSubscription("weather-requests");

    api.WithReference(serviceBus).WaitFor(serviceBus);
    worker.WithReference(serviceBus).WaitFor(serviceBus);
}
else if (messagingProvider == "Sqs")
{
    var localstack = builder.AddContainer("sqs", "localstack/localstack", "4.4.0")
        .WithEnvironment("SERVICES", "sqs,sns")
        .WithHttpEndpoint(targetPort: 4566, name: "sqs")
        .WithLifetime(ContainerLifetime.Persistent);

    var sqsEndpoint = localstack.GetEndpoint("sqs");
    api.WithEnvironment("ConnectionStrings__sqs", sqsEndpoint).WaitFor(localstack);
    worker.WithEnvironment("ConnectionStrings__sqs", sqsEndpoint).WaitFor(localstack);
}

builder.Build().Run();
