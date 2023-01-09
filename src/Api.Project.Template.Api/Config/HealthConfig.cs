using System;
using Api.Project.Template.Infrastructure.Data;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Project.Template.Api.Config;

public static class HealthConfig
{
    public static void AddHealthCheckConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<Context>()
            .AddSqlServer(configuration.GetConnectionString("Database") ?? throw new InvalidOperationException());
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
