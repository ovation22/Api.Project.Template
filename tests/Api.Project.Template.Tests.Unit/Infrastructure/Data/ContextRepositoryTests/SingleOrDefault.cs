using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class SingleOrDefault : ContextRepositoryTestBase
{
    private readonly int _existingId;

    public SingleOrDefault(ContextFixture fixture) : base(fixture)
    {
        _existingId = Context.WeatherForecasts.First().Id;
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        // Act
        var result = await Repository.SingleOrDefaultAsync<WeatherForecast>(
            x => x.Id == _existingId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_existingId, result.Id);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        // Act
        var result = await Repository.SingleOrDefaultAsync<WeatherForecast>(
            x => x.Id == int.MaxValue,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var spec = new WeatherForecastByIdSpec(_existingId);

        // Act
        var result = await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_existingId, result.Id);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        var spec = new WeatherForecastByIdSpec(int.MaxValue);

        // Act
        var result = await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenEntityExists_ReturnsMappedResult()
    {
        // Arrange
        var spec = new WeatherForecastIdProjectionSpec(_existingId);

        // Act
        var result = await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(_existingId, result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenEntityDoesNotExist_ReturnsDefault()
    {
        // Arrange
        var spec = new WeatherForecastIdProjectionSpec(int.MaxValue);

        // Act
        var result = await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(default, result);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenMultipleEntitiesMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 2 },
            new WeatherForecast { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 2 }
        );
        Context.SaveChanges();

        // Act
        var act = async () => await Repository.SingleOrDefaultAsync<WeatherForecast>(
            x => x.SummaryId == 2,
            TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenMultipleEntitiesMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 3 },
            new WeatherForecast { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 3 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastBySummarySpec(3);

        // Act
        var act = async () => await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenMultipleEntitiesMatch_ThrowsInvalidOperationException()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 6, 1), TemperatureC = 10, SummaryId = 4 },
            new WeatherForecast { Date = new DateOnly(2025, 6, 2), TemperatureC = 12, SummaryId = 4 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastIdsBySummarySpec(4);

        // Act
        var act = async () => await Repository.SingleOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private sealed class WeatherForecastBySummarySpec : Specification<WeatherForecast>
    {
        public WeatherForecastBySummarySpec(int summaryId) =>
            Query.Where(x => x.SummaryId == summaryId);
    }

    private sealed class WeatherForecastIdsBySummarySpec : Specification<WeatherForecast, int>
    {
        public WeatherForecastIdsBySummarySpec(int summaryId) =>
            Query.Where(x => x.SummaryId == summaryId).Select(x => x.Id);
    }

    private sealed class WeatherForecastByIdSpec : Specification<WeatherForecast>
    {
        public WeatherForecastByIdSpec(int id)
        {
            Query.Where(x => x.Id == id);
        }
    }

    private sealed class WeatherForecastIdProjectionSpec : Specification<WeatherForecast, int>
    {
        public WeatherForecastIdProjectionSpec(int id)
        {
            Query.Where(x => x.Id == id).Select(x => x.Id);
        }
    }
}
