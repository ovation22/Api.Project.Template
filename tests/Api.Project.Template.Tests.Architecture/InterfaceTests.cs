using Api.Project.Template.Core.Interfaces.Logging;
using NetArchTest.Rules;
using Xunit;

namespace Api.Project.Template.Tests.Architecture;

public class InterfaceTests
{
    private readonly Types _types;

    public InterfaceTests()
    {
        _types = Types.InAssembly(typeof(ILoggerAdapter<>).Assembly);
    }

    [Fact]
    public void Interfaces_ShouldStartWithI()
    {
        var result = _types
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
