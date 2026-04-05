using Api.Project.Template.Domain.Entities;
using Api.Project.Template.Infrastructure.Data.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Infrastructure.Data;

public class ApiProjectTemplateContext : DbContext
{
    public ApiProjectTemplateContext(DbContextOptions<ApiProjectTemplateContext> options) : base(options)
    {
    }

    public virtual DbSet<WeatherSummary> WeatherSummaries { get; set; } = null!;

    public virtual DbSet<WeatherForecast> WeatherForecasts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiProjectTemplateContext).Assembly);

        modelBuilder.Seed();
    }
}
