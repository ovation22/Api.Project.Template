using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Domain.Entities;
using NetArchTest.Rules;

namespace Api.Project.Template.Tests.Architecture;

public class LayerTests
{
    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ILoggerAdapter<>).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void Domain_ShouldNotReference_Application()
    {
        var result = Types.InAssembly(typeof(WeatherForecast).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Application")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void Domain_ShouldNotReference_Infrastructure()
    {
        var result = Types.InAssembly(typeof(WeatherForecast).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Api.Project.Template.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
