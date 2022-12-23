using Api.Project.Template.Core.Models.Entities;
using Xunit;

namespace Api.Project.Template.Tests.Unit.Infrastructure.Data.ContextRepositoryTests;

[Collection("ContextFixture")]
[Trait("Category", "ContextRepository")]
public class Get : ContextRepositoryTestBase
{
    private readonly Guid _id;

    public Get(ContextFixture fixture) : base(fixture)
    {
        _id = Context.WeatherForecasts.First().Id;
    }

    [Fact]
    public async Task ItReturnsSpeaker()
    {
        // Arrange
        // Act
        var result = await Repository.Get<WeatherForecast>(x => x.Id == _id);

        // Assert
        Assert.IsAssignableFrom<WeatherForecast>(result);
    }
}
