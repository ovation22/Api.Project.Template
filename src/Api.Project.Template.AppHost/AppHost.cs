var builder = DistributedApplication.CreateBuilder(args);

var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

var project = builder.AddProject<Projects.Api_Project_Template_Api>("api-project-template-api")
    .WithEnvironment("DatabaseProvider", provider);

if (provider == "PostgreSQL")
{
    var pg = builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("ApiProjectTemplate");

    project.WithReference(pg).WaitFor(pg);
}
else
{
    var sql = builder.AddSqlServer("sql", port: 59944)
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent)
        .AddDatabase("ApiProjectTemplate");

    project.WithReference(sql).WaitFor(sql);
}

builder.Build().Run();
