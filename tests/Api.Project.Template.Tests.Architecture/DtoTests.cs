using Api.Project.Template.Application.Features.Weather.Queries;
using NetArchTest.Rules;

namespace Api.Project.Template.Tests.Architecture;

public class DtoTests
{
    private readonly Types _types;

    public DtoTests()
    {
        _types = Types.InAssembly(typeof(GetWeatherForecastsResponse).Assembly);
    }

    [Fact]
    public void ResponseTypes_ShouldNotReference_DomainEntities()
    {
        // Arrange
        var rule = _types
            .That()
            .HaveNameEndingWith("Response")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Domain.Entities");

        // Act
        var result = rule.GetResult();

        // Assert
        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
