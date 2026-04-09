using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Count : ContextRepositoryTestBase
{
    public Count(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "NoFilter")]
    public async Task WithNoFilter_WhenEntitiesExist_ReturnsCount()
    {
        // Arrange
        var expected = Context.WeatherForecasts.Count();

        // Act
        var result = await Repository.CountAsync<WeatherForecast>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    [Trait("Overload", "NoFilter")]
    public async Task WithNoFilter_WhenNoEntitiesExist_ReturnsZero()
    {
        // Arrange
        Context.WeatherForecasts.RemoveRange(Context.WeatherForecasts);
        Context.SaveChanges();

        // Act
        var result = await Repository.CountAsync<WeatherForecast>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntitiesMatch_ReturnsMatchingCount()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 2 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 2 }
        );
        Context.SaveChanges();

        // Act
        var result = await Repository.CountAsync<WeatherForecast>(
            x => x.SummaryId == 2,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenNoEntitiesMatch_ReturnsZero()
    {
        // Arrange
        // Act
        var result = await Repository.CountAsync<WeatherForecast>(
            x => x.Id == -1,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntitiesMatch_ReturnsMatchingCount()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 3 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 3 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastBySummarySpec(3);

        // Act
        var result = await Repository.CountAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenNoEntitiesMatch_ReturnsZero()
    {
        // Arrange
        var spec = new WeatherForecastBySummarySpec(-1);

        // Act
        var result = await Repository.CountAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, result);
    }

    private sealed class WeatherForecastBySummarySpec : Specification<WeatherForecast>
    {
        public WeatherForecastBySummarySpec(int summaryId) =>
            Query.Where(x => x.SummaryId == summaryId);
    }
}
