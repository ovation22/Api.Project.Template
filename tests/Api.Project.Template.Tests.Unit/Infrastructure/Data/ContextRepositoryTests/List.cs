using Api.Project.Template.Application.Common.Pagination;
using Api.Project.Template.Application.Common.Specifications;
using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class List : ContextRepositoryTestBase
{
    public List(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntitiesMatch_ReturnsList()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 2 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 2 }
        );
        Context.SaveChanges();

        // Act
        var result = await Repository.ListAsync<WeatherForecast>(
            x => x.SummaryId == 2,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(2, x.SummaryId));
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenNoEntitiesMatch_ReturnsEmptyList()
    {
        // Arrange
        // Act
        var result = await Repository.ListAsync<WeatherForecast>(
            x => x.Id == -1,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntitiesMatch_ReturnsList()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 3 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 3 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastBySummarySpec(3);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(3, x.SummaryId));
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenNoEntitiesMatch_ReturnsEmptyList()
    {
        // Arrange
        var spec = new WeatherForecastBySummarySpec(-1);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenEntitiesMatch_ReturnsProjectedList()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 4 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 4 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastTemperatureBySummarySpec(4);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(10, result);
        Assert.Contains(12, result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenSelectorIsNull_ReturnsEmptyList()
    {
        // Arrange
        var spec = new WeatherForecastNullSelectorSpec();

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T, TResult>")]
    public async Task WithProjectingSpecification_WhenNoEntitiesMatch_ReturnsEmptyList()
    {
        // Arrange
        var spec = new WeatherForecastTemperatureBySummarySpec(-1);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Overload", "GroupBy")]
    public async Task WithGroupBy_WhenEntitiesMatch_ReturnsGroupedResults()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 10, SummaryId = 5 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 2), TemperatureC = 12, SummaryId = 5 },
            new WeatherForecast { Date = new DateOnly(2025, 1, 3), TemperatureC = 8,  SummaryId = 6 }
        );
        Context.SaveChanges();
        var spec = new WeatherForecastBySummaryRangeSpec(5, 6);

        // Act
        var result = await Repository.ListAsync<WeatherForecast, int, ForecastCountBySummary>(
            spec,
            x => x.SummaryId,
            g => new ForecastCountBySummary(g.Key, g.Count()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.SummaryId == 5 && x.Count == 2);
        Assert.Contains(result, x => x.SummaryId == 6 && x.Count == 1);
    }

    [Fact]
    [Trait("Overload", "GroupBy")]
    public async Task WithGroupBy_WhenNoEntitiesMatch_ReturnsEmptyList()
    {
        // Arrange
        var spec = new WeatherForecastBySummarySpec(-1);

        // Act
        var result = await Repository.ListAsync<WeatherForecast, int, ForecastCountBySummary>(
            spec,
            x => x.SummaryId,
            g => new ForecastCountBySummary(g.Key, g.Count()),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    [Trait("Overload", "PaginatedSpecification<T>")]
    public async Task WithPaginatedSpecification_ReturnsCorrectPage()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(Enumerable.Range(1, 5).Select(i =>
            new WeatherForecast { Date = new DateOnly(2025, 1, i), TemperatureC = i, SummaryId = 7 }));
        Context.SaveChanges();
        var spec = new WeatherForecastPagedBySummarySpec(7, pageNumber: 1, pageSize: 2);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Data.Count());
        Assert.Equal(5, result.Total);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.Size);
    }

    [Fact]
    [Trait("Overload", "PaginatedSpecification<T>")]
    public async Task WithPaginatedSpecification_WhenPageExceedsData_ReturnsEmptyItems()
    {
        // Arrange
        Context.WeatherForecasts.Add(
            new WeatherForecast { Date = new DateOnly(2025, 1, 1), TemperatureC = 5, SummaryId = 8 });
        Context.SaveChanges();
        var spec = new WeatherForecastPagedBySummarySpec(8, pageNumber: 2, pageSize: 10);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Data);
        Assert.Equal(1, result.Total);
    }

    [Fact]
    [Trait("Overload", "PaginatedSpecification<T, TResult>")]
    public async Task WithProjectedPaginatedSpecification_ReturnsCorrectProjectedPage()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(Enumerable.Range(1, 5).Select(i =>
            new WeatherForecast { Date = new DateOnly(2025, 1, i), TemperatureC = i * 5, SummaryId = 9 }));
        Context.SaveChanges();
        var spec = new WeatherForecastTemperaturePagedSpec(9, pageNumber: 1, pageSize: 3);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Data.Count());
        Assert.Equal(5, result.Total);
    }

    [Fact]
    [Trait("Overload", "PaginatedSpecification<T, TResult>")]
    public async Task WithProjectedPaginatedSpecification_WhenNoEntitiesMatch_ReturnsEmptyPage()
    {
        // Arrange
        var spec = new WeatherForecastTemperaturePagedSpec(-1, pageNumber: 1, pageSize: 10);

        // Act
        var result = await Repository.ListAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Data);
        Assert.Equal(0, result.Total);
    }

    private record ForecastCountBySummary(int SummaryId, int Count);

    private sealed class WeatherForecastBySummarySpec : Specification<WeatherForecast>
    {
        public WeatherForecastBySummarySpec(int summaryId) =>
            Query.Where(x => x.SummaryId == summaryId);
    }

    private sealed class WeatherForecastBySummaryRangeSpec : Specification<WeatherForecast>
    {
        public WeatherForecastBySummaryRangeSpec(int from, int to) =>
            Query.Where(x => x.SummaryId >= from && x.SummaryId <= to);
    }

    private sealed class WeatherForecastTemperatureBySummarySpec : Specification<WeatherForecast, int>
    {
        public WeatherForecastTemperatureBySummarySpec(int summaryId) =>
            Query.Where(x => x.SummaryId == summaryId).Select(x => x.TemperatureC);
    }

    private sealed class WeatherForecastNullSelectorSpec : Specification<WeatherForecast, int>
    {
        // Selector intentionally omitted to exercise the null-selector guard in ListAsync
    }

    private sealed class WeatherForecastPagedBySummarySpec : PaginatedSpecification<WeatherForecast>
    {
        public WeatherForecastPagedBySummarySpec(int summaryId, int pageNumber, int pageSize)
            : base(pageNumber, pageSize) =>
            Query.Where(x => x.SummaryId == summaryId);
    }

    private sealed class WeatherForecastTemperaturePagedSpec : PaginatedSpecification<WeatherForecast, int>
    {
        public WeatherForecastTemperaturePagedSpec(int summaryId, int pageNumber, int pageSize)
            : base(pageNumber, pageSize) =>
            Query.Where(x => x.SummaryId == summaryId).Select(x => x.TemperatureC);
    }
}
