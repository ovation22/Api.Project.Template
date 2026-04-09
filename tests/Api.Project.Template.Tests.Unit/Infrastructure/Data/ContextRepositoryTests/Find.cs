using Api.Project.Template.Domain.Entities;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Trait("Category", "ContextRepository")]
public class Find : ContextRepositoryTestBase
{
    private readonly int _existingId;

    public Find(ContextFixture fixture) : base(fixture)
    {
        _existingId = Context.WeatherForecasts.First().Id;
    }

    [Fact]
    public async Task WhenIdIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        object? nullId = null;

        // Act
        var act = async () => await Repository.FindAsync<WeatherForecast>(nullId!, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task WhenEntityExists_ReturnsEntity()
    {
        // Arrange
        // Act
        var result = await Repository.FindAsync<WeatherForecast>(_existingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_existingId, result.Id);
    }

    [Fact]
    public async Task WhenEntityDoesNotExist_ReturnsNull()
    {
        // Arrange
        var missingId = int.MaxValue;

        // Act
        var result = await Repository.FindAsync<WeatherForecast>(missingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }
}
