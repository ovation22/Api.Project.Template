using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class FirstOrDefault : ContextRepositoryTestBase
{
    private readonly int _id;

    public FirstOrDefault(ContextFixture fixture) : base(fixture)
    {
        _id = Context.WeatherForecasts.First().Id;
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        // Act
        var result = await Repository.FirstOrDefaultAsync<WeatherForecast>(
            x => x.Id == _id,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_id, result.Id);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        // Act
        var result = await Repository.FirstOrDefaultAsync<WeatherForecast>(
            x => x.Id == int.MaxValue,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Overload", "Expression+OrderBy")]
    public async Task WithExpressionAndOrderBy_WhenMultipleEntitiesMatch_ReturnsFirstByOrder()
    {
        // Arrange
        Context.WeatherForecasts.AddRange(
            new WeatherForecast { Date = new DateOnly(2025, 6, 1), TemperatureC = 15, SummaryId = 3 },
            new WeatherForecast { Date = new DateOnly(2025, 6, 2), TemperatureC = 5, SummaryId = 3 }
        );
        Context.SaveChanges();

        // Act
        var result = await Repository.FirstOrDefaultAsync<WeatherForecast>(
            x => x.SummaryId == 3,
            q => q.OrderBy(x => x.TemperatureC),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TemperatureC);
    }

    [Fact]
    [Trait("Overload", "Expression+OrderBy")]
    public async Task WithExpressionAndOrderBy_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        // Act
        var result = await Repository.FirstOrDefaultAsync<WeatherForecast>(
            x => x.Id == int.MaxValue,
            q => q.OrderBy(x => x.TemperatureC),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        var spec = new WeatherForecastByIdSpec(_id);

        // Act
        var result = await Repository.FirstOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_id, result.Id);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        var spec = new WeatherForecastByIdSpec(int.MaxValue);

        // Act
        var result = await Repository.FirstOrDefaultAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    private sealed class WeatherForecastByIdSpec : Specification<WeatherForecast>
    {
        public WeatherForecastByIdSpec(int id)
        {
            Query.Where(x => x.Id == id);
        }
    }
}
