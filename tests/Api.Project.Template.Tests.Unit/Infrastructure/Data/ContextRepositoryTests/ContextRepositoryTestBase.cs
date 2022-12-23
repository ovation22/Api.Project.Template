using Api.Project.Template.Core.Models.Entities;
using Api.Project.Template.Infrastructure.Data;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

public class ContextRepositoryTestBase : IClassFixture<ContextFixture>
{
    protected readonly Context Context;
    protected readonly ContextRepository Repository;

    public ContextRepositoryTestBase(ContextFixture fixture)
    {
        Context = fixture.Context;

        Context.WeatherForecasts.Add(new WeatherForecast
        {
            Id = Guid.NewGuid(),
            Date = DateTime.UtcNow,
            Summary = "Scorching",
            TemperatureC = 21
        });

        Context.SaveChanges();

        Repository = new ContextRepository(Context);
    }
}
