using Api.Project.Template.Domain.Entities;
using NetArchTest.Rules;

namespace Api.Project.Template.Tests.Architecture;

public class DtoTests
{
    private readonly Types _types;

    public DtoTests()
    {
        _types = Types.InAssembly(typeof(WeatherForecast).Assembly);
    }

    [Fact]
    public void Dto_ShouldNotReference_Entities()
    {
        // Arrange
        var rule = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Domain.DTOs")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Domain.Entities");

        // Act
        var result = rule.GetResult();

        // Assert
        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
