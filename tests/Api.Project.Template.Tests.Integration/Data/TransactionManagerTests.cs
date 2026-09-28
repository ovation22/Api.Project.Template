using Api.Project.Template.Domain.Entities;
using Api.Project.Template.Infrastructure.Data;
using Api.Project.Template.Infrastructure.Logging;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Api.Project.Template.Tests.Integration.Data;

/// <summary>
/// Runs TransactionManager against SQLite with a retrying execution strategy â€” the same kind of
/// strategy Aspire's AddSqlServerDbContext/AddNpgsqlDbContext enable by default.
/// </summary>
[Trait("Category", "TransactionManager")]
public sealed class TransactionManagerTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        await using var context = CreateContext(retrying: false);
        await context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();

    [Fact]
    public async Task ExecuteAsync_WithRetryingStrategy_CommitsOperation()
    {
        // Arrange
        await using var context = CreateContext(retrying: true);
        var manager = CreateManager(context);

        // Act
        await manager.ExecuteAsync(async () =>
        {
            context.WeatherForecasts.Add(NewForecast(day: 11));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }, TestContext.Current.CancellationToken);

        // Assert
        (await CountForecastsAsync(day: 11)).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientFailure_RetriesWholeOperation()
    {
        // Arrange
        await using var context = CreateContext(retrying: true);
        var manager = CreateManager(context);
        var attempts = 0;

        // Act
        var result = await manager.ExecuteAsync(async () =>
        {
            attempts++;
            if (attempts == 1)
                throw new TransientTestException();

            context.WeatherForecasts.Add(NewForecast(day: 12));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return attempts;
        }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(2);
        (await CountForecastsAsync(day: 12)).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationThrows_RollsBackAndRethrows()
    {
        // Arrange
        await using var context = CreateContext(retrying: true);
        var manager = CreateManager(context);

        // Act
        var act = () => manager.ExecuteAsync(async () =>
        {
            context.WeatherForecasts.Add(NewForecast(day: 13));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException("boom");
        }, TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        (await CountForecastsAsync(day: 13)).Should().Be(0);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithRetryingStrategy_ThrowsWithGuidance()
    {
        // Arrange
        await using var context = CreateContext(retrying: true);
        var manager = CreateManager(context);

        // Act
        var act = () => manager.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Use ExecuteAsync*");
    }

    [Fact]
    public async Task BeginTransactionAsync_WithDefaultStrategy_CommitsChanges()
    {
        // Arrange
        await using var context = CreateContext(retrying: false);
        var manager = CreateManager(context);

        // Act
        await manager.BeginTransactionAsync(TestContext.Current.CancellationToken);
        context.WeatherForecasts.Add(NewForecast(day: 14));
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await manager.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        (await CountForecastsAsync(day: 14)).Should().Be(1);
    }

    private ApiProjectTemplateContext CreateContext(bool retrying)
    {
        var options = new DbContextOptionsBuilder<ApiProjectTemplateContext>()
            .UseSqlite(_connection, sqlite =>
            {
                if (retrying)
                    sqlite.ExecutionStrategy(dependencies => new TestRetryingExecutionStrategy(dependencies));
            })
            .Options;

        return new ApiProjectTemplateContext(options);
    }

    private static TransactionManager CreateManager(ApiProjectTemplateContext context)
        => new(context, new LoggerAdapter<TransactionManager>(NullLogger<TransactionManager>.Instance));

    // Each test uses its own date (outside the Jan 2025 seed data) to identify its row.
    private static WeatherForecast NewForecast(int day)
        => new() { Date = new DateOnly(2030, 1, day), TemperatureC = 20, SummaryId = 1 };

    // Read through a separate context so the check sees only what was committed.
    private async Task<int> CountForecastsAsync(int day)
    {
        await using var context = CreateContext(retrying: false);
        var date = new DateOnly(2030, 1, day);
        return await context.WeatherForecasts.CountAsync(
            x => x.Date == date, TestContext.Current.CancellationToken);
    }

    private sealed class TransientTestException : Exception;

    private sealed class TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : ExecutionStrategy(dependencies, maxRetryCount: 3, maxRetryDelay: TimeSpan.Zero)
    {
        protected override bool ShouldRetryOn(Exception exception) => exception is TransientTestException;
    }
}
