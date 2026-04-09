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
    var serviceBus = builder.AddAzureServiceBus("servicebus");
    api.WithReference(serviceBus);
    worker.WithReference(serviceBus);
}

builder.Build().Run();
