using Api.Project.Template.Core.Models.DTO;
using NetArchTest.Rules;
using Xunit;

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
        var result = _types
            .That()
            .ResideInNamespace("Api.Project.Template.Core.Models.DTO")
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Core.Models.Entities")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
