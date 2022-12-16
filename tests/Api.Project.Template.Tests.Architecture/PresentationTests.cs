using Api.Project.Template.Api;
using NetArchTest.Rules;
using Xunit;

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
        var result = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Core.Models.Entities")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void Controllers_ShouldNotReference_Data()
    {
        var result = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Infrastructure.Data")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
