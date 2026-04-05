using Api.Project.Template.Api;
using NetArchTest.Rules;

namespace Api.Project.Template.Tests.Architecture;

public class PresentationTests
{
    private readonly Types _types;

    public PresentationTests()
    {
        _types = Types.InAssembly(typeof(Program).Assembly);
    }

    [Fact]
    public void Controllers_ShouldNotReference_Entities()
    {
        // Arrange
        var rule = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Domain.Entities");

        // Act
        var result = rule.GetResult();

        // Assert
        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void Controllers_ShouldNotReference_Data()
    {
        // Arrange
        var rule = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Infrastructure.Data");

        // Act
        var result = rule.GetResult();

        // Assert
        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
