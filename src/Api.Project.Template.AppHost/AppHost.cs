var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql", port: 59944)
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("ApiProjectTemplate");

builder.AddProject<Projects.Api_Project_Template_Api>("api-project-template-api")
    .WithReference(sql)
    .WaitFor(sql);

builder.Build().Run();
