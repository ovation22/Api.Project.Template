using Api.Project.Template.Application.Abstractions.Logging;
using Api.Project.Template.Domain.Entities;
using Api.Project.Template.Infrastructure;
using NetArchTest.Rules;

namespace Api.Project.Template.Tests.Architecture;

public class InterfaceTests
{
    [Fact]
    public void ApplicationInterfaces_ShouldStartWithI()
    {
        var result = Types.InAssembly(typeof(ILoggerAdapter<>).Assembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void DomainInterfaces_ShouldStartWithI()
    {
        var result = Types.InAssembly(typeof(WeatherForecast).Assembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }

    [Fact]
    public void InfrastructureInterfaces_ShouldStartWithI()
    {
        var result = Types.InAssembly(typeof(DependencyInjection).Assembly)
            .That().AreInterfaces()
            .Should().HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
