using Api.Project.Template.Infrastructure.Data;
using Serilog;

namespace Api.Project.Template.Api.Config;

public static class DatabaseConfig
{
    public static void AddSqlServerDatabaseContext(this IHostApplicationBuilder builder)
    {
        builder.AddSqlServerDbContext<ApiProjectTemplateContext>("ApiProjectTemplate");
    }

    public static void AddPostgreSqlDatabaseContext(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<ApiProjectTemplateContext>("ApiProjectTemplate");
    }

    public static void EnsureDatabaseCreated(this WebApplication app)
    {
        // Ensure the database is created and seed data is applied on first run.
        // EnsureCreated() is used here for simplicity — it creates the schema from the
        // current model if the database does not exist, and applies HasData() seed data.
        // NOTE: EnsureCreated() does NOT apply EF migrations. It is not compatible with
        // Migration-based workflows. When you are ready to switch to migrations:
        //
        //   1. Remove the EnsureCreated() call below.
        //   2. Generate an initial migration:
        //        dotnet ef migrations add InitialCreate \
        //          --project src/Api.Project.Template.Infrastructure \
        //          --startup-project src/Api.Project.Template.Api
        //   3. Replace EnsureCreated() with MigrateAsync():
        //        await db.Database.MigrateAsync();
        using var scope = app.Services.CreateScope();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApiProjectTemplateContext>();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while creating the database.");
            throw;
        }
    }
}
