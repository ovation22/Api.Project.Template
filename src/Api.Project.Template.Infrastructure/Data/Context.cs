using Api.Project.Template.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Project.Template.Infrastructure.Data;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }

    public virtual DbSet<WeatherForecast> WeatherForecasts { get; set; } = null!;
}
