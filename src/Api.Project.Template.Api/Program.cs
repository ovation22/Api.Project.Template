using Api.Project.Template.Api.Config;
using Api.Project.Template.Api.Middleware;
using Api.Project.Template.Application;
using Api.Project.Template.Infrastructure;
using Api.Project.Template.Infrastructure.Data;
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

            builder.AddSqlServerDbContext<ApiProjectTemplateContext>("ApiProjectTemplate");

            builder.Services.AddInfrastructure();
            builder.Services.AddApplication();

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

            // Ensure the database is created and seed data is applied on first run.
            // EnsureCreated() is used here for simplicity — it creates the schema from the
            // current model if the database does not exist, and applies HasData() seed data.
            // NOTE: EnsureCreated() does NOT apply EF migrations. It is not compatible with
            // Migration-based workflows. When you are ready to switch to migrations:
            //
            //   1. Remove the EnsureCreated() block below.
            //   2. Generate an initial migration:
            //        dotnet ef migrations add InitialCreate \
            //          --project src/Api.Project.Template.Infrastructure \
            //          --startup-project src/Api.Project.Template.Api
            //   3. Replace EnsureCreated() with MigrateAsync():
            //        await db.Database.MigrateAsync();
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<ApiProjectTemplateContext>();
                    db.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "An error occurred while creating the database.");
                    throw;
                }
            }

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
