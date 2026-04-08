using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Api.Project.Template.Api.Config;

public static class HealthConfig
{
    public static void AddSqlServerHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApiProjectTemplate")
            ?? throw new InvalidOperationException("Connection string 'ApiProjectTemplate' is not configured.");

        services.AddHealthChecks()
            .AddSqlServer(connectionString);
    }

    public static void AddPostgreSqlHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ApiProjectTemplate")
            ?? throw new InvalidOperationException("Connection string 'ApiProjectTemplate' is not configured.");

        services.AddHealthChecks()
            .AddNpgSql(connectionString);
    }

    public static void UseHealthCheckConfig(this WebApplication app)
    {
        app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
    }
}
