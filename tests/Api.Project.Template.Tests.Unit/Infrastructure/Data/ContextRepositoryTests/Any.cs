using Api.Project.Template.Domain.Entities;
using Ardalis.Specification;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Any : ContextRepositoryTestBase
{
    public Any(ContextFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [Trait("Overload", "NoFilter")]
    public async Task WithNoFilter_WhenEntitiesExist_ReturnsTrue()
    {
        // Arrange
        // Act
        var result = await Repository.AnyAsync<WeatherForecast>(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Overload", "NoFilter")]
    public async Task WithNoFilter_WhenNoEntitiesExist_ReturnsFalse()
    {
        // Arrange
        Context.WeatherForecasts.RemoveRange(Context.WeatherForecasts);
        Context.SaveChanges();

        // Act
        var result = await Repository.AnyAsync<WeatherForecast>(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenMatchingEntityExists_ReturnsTrue()
    {
        // Arrange
        var existingId = Context.WeatherForecasts.First().Id;

        // Act
        var result = await Repository.AnyAsync<WeatherForecast>(
            x => x.Id == existingId,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Overload", "Expression")]
    public async Task WithExpression_WhenNoMatchingEntityExists_ReturnsFalse()
    {
        // Arrange
        // Act
        var result = await Repository.AnyAsync<WeatherForecast>(
            x => x.Id == -1,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenMatchingEntityExists_ReturnsTrue()
    {
        // Arrange
        var existingId = Context.WeatherForecasts.First().Id;
        var spec = new WeatherForecastByIdSpec(existingId);

        // Act
        var result = await Repository.AnyAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Overload", "ISpecification<T>")]
    public async Task WithSpecification_WhenNoMatchingEntityExists_ReturnsFalse()
    {
        // Arrange
        var spec = new WeatherForecastByIdSpec(-1);

        // Act
        var result = await Repository.AnyAsync(spec, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    private sealed class WeatherForecastByIdSpec : Specification<WeatherForecast>
    {
        public WeatherForecastByIdSpec(int id) =>
            Query.Where(x => x.Id == id);
    }
}
