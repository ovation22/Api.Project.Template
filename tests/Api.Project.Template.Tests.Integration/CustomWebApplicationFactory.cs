using Api.Project.Template.Api;
using Api.Project.Template.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Held open for the factory lifetime — in-memory SQLite databases are destroyed
    // when the last connection to them closes.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy connection string so Aspire's AddSqlServerDbContext
        // call in Program.cs doesn't throw on missing configuration.
        builder.UseSetting("ConnectionStrings:ApiProjectTemplate", "Server=dummy");

        // ConfigureTestServices runs after all app services are registered,
        // ensuring our overrides take precedence over Aspire's registrations.
        builder.ConfigureTestServices(services =>
        {
            // Aspire's AddSqlServerDbContext registers a DbContext pool (singleton) in
            // addition to the context and options. We remove everything whose generic type
            // arguments include ApiProjectTemplateContext — options, pool, scoped lease, and
            // the context itself — before adding the plain SQLite replacement.
            var contextType = typeof(ApiProjectTemplateContext);
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == contextType ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GenericTypeArguments.Any(t => t == contextType)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            // Replace with a plain scoped DbContext backed by SQLite on the shared connection.
            services.AddDbContext<ApiProjectTemplateContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
