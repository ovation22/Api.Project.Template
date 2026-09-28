using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Api.Project.Template.Api.Config;

public static class HealthConfig
{
    // The database check comes from Aspire's AddSqlServerDbContext / AddNpgsqlDbContext (one per DbContext),
    // so no provider-specific health check is registered here.

    public static void UseHealthCheckConfig(this WebApplication app)
    {
        // The detailed JSON (check names, durations, exception messages) is only returned in Development;
        // elsewhere the endpoint answers with just Healthy / Degraded / Unhealthy.
        var readiness = new HealthCheckOptions();
        if (app.Environment.IsDevelopment())
        {
            readiness.ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse;
        }

        app.MapHealthChecks("/healthz/ready", readiness);

        app.MapHealthChecks("/healthz/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });
    }
}
