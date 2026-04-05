using Api.Project.Template.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Project.Template.Infrastructure.Data.Configurations;

public class WeatherSummaryConfiguration : IEntityTypeConfiguration<WeatherSummary>
{
    public void Configure(EntityTypeBuilder<WeatherSummary> builder)
    {
        builder.ToTable("WeatherSummaries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();
    }
}
