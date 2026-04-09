using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Domain.Entities;
using Api.Project.Template.Infrastructure.Data;
using Api.Project.Template.Infrastructure.Data.Repositories;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

public class ContextRepositoryTestBase : IClassFixture<ContextFixture>
{
    protected readonly ApiProjectTemplateContext Context;
    protected readonly ApiProjectTemplateContextRepository Repository;

    public ContextRepositoryTestBase(ContextFixture fixture)
    {
        Context = fixture.Context;

        Context.WeatherForecasts.Add(new WeatherForecast
        {
            Date = new DateOnly(2025, 1, 1),
            TemperatureC = 21,
            SummaryId = 10
        });

        Context.SaveChanges();

        var loggerMock = new Mock<ILoggerAdapter<EFRepository>>();
        Repository = new ApiProjectTemplateContextRepository(Context, loggerMock.Object);
    }
}
