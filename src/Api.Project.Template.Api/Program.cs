using Api.Project.Template.Api.Config;
using Api.Project.Template.Api.Middleware;
using Api.Project.Template.Application;
using Api.Project.Template.Infrastructure;
using Api.Project.Template.ServiceDefaults;
using Scalar.AspNetCore;
using Serilog;

namespace Api.Project.Template.Api;

public class Program
{
    public static int Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            builder.Host.UseSerilog(Log.Logger);

            Log.Information("Starting Api.Project.Template.Api");

            builder.Services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });

            var isPostgreSql = builder.Configuration["DatabaseProvider"] == "PostgreSQL";

            if (isPostgreSql)
                builder.AddPostgreSqlDatabaseContext();
            else
                builder.AddSqlServerDatabaseContext();

            builder.Services.AddInfrastructure();
            builder.Services.AddApplication();

            if (isPostgreSql)
                builder.Services.AddPostgreSqlHealthChecks(builder.Configuration);
            else
                builder.Services.AddSqlServerHealthChecks(builder.Configuration);

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddProblemDetails();

            builder.Services.AddControllers();

            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Weather Forecast API";
                    document.Info.Version = "v1";
                    document.Info.Description = "A sample .NET API with Clean Architecture";
                    return Task.CompletedTask;
                });
            });

            var app = builder.Build();

            app.EnsureDatabaseCreated();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseHealthCheckConfig();

            app.UseSerilogRequestLogging(x =>
            {
                x.GetLevel = (httpContext, elapsed, ex) =>
                httpContext.Request.Path.StartsWithSegments("/healthz")
                    ? Serilog.Events.LogEventLevel.Verbose
                    : ex is not null ? Serilog.Events.LogEventLevel.Error :
                    Serilog.Events.LogEventLevel.Information;
            });

            app.MapControllers();

            app.Run();

            return 0;
        }
        catch (Exception ex) when (ex is not HostAbortedException)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.Information("Shutting down Api.Project.Template.Api");
            Log.CloseAndFlush();
        }
    }
}
