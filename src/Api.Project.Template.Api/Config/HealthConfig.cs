using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Api.Config;

public static class HealthConfig
{
    public static void AddHealthCheckConfig(this IServiceCollection services)
    {
        services.AddHealthChecks();
    }

    public static void UseHealthCheckConfig(this WebApplication app)
    {
        app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready")
        });

        app.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
    }
}