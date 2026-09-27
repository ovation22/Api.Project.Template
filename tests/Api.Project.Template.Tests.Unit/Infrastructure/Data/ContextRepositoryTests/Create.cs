using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Domain.Entities;
using Api.Project.Template.Infrastructure.Data;
using Api.Project.Template.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Create : ContextRepositoryTestBase
{
    public Create(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_PersistsEntity()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 1),
            TemperatureC = 18,
            SummaryId = 5
        };

        // Act
        await Repository.CreateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(Context.WeatherForecasts, x => x == weatherForecast);
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenSaveIsCanceled_RethrowsOperationCanceledException()
    {
        // Arrange
        // Relational providers surface cancellation as OperationCanceledException (not TaskCanceledException),
        // so simulate that directly rather than relying on the in-memory provider's behavior.
        var options = new DbContextOptionsBuilder<ApiProjectTemplateContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new CancelingSaveChangesInterceptor())
            .Options;
        await using var context = new ApiProjectTemplateContext(options);
        var repository = new ApiProjectTemplateContextRepository(context, new Mock<ILoggerAdapter<EFRepository>>().Object);
        var weatherForecast = new WeatherForecast { Date = new DateOnly(2025, 6, 3), TemperatureC = 16, SummaryId = 5 };

        // Act
        var act = () => repository.CreateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        // Cancellation must surface as-is, not wrapped in a generic Exception (which would become a 500).
        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    private sealed class CancelingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => throw new OperationCanceledException();
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_ReturnsPersistedEntity()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 2),
            TemperatureC = 17,
            SummaryId = 5
        };

        // Act
        var result = await Repository.CreateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(weatherForecast, result);
    }

    [Fact]
    [Trait("Overload", "Single")]
    public async Task Single_WhenCalled_AssignsGeneratedId()
    {
        // Arrange
        var weatherForecast = new WeatherForecast
        {
            Date = new DateOnly(2025, 6, 3),
            TemperatureC = 20,
            SummaryId = 5
        };

        // Act
        await Repository.CreateAsync(weatherForecast, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotEqual(0, weatherForecast.Id);
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_PersistsAllEntities()
    {
        // Arrange
        var weatherForecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 6 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 6 },
            new() { Date = new DateOnly(2025, 6, 3), TemperatureC = 14, SummaryId = 6 }
        };

        // Act
        await Repository.CreateAsync(weatherForecasts, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(weatherForecasts, f => Assert.Contains(Context.WeatherForecasts, x => x == f));
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_ReturnsAllPersistedEntities()
    {
        // Arrange
        var weatherForecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 7 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 7 }
        };

        // Act
        var result = await Repository.CreateAsync(weatherForecasts, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(weatherForecasts, result);
    }

    [Fact]
    [Trait("Overload", "Bulk")]
    public async Task Bulk_WhenCalled_AssignsGeneratedIdToEachEntity()
    {
        // Arrange
        var weatherForecasts = new List<WeatherForecast>
        {
            new() { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 8 },
            new() { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 8 }
        };

        // Act
        await Repository.CreateAsync(weatherForecasts, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(weatherForecasts, f => Assert.NotEqual(0, f.Id));
    }
}
