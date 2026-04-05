using Api.Project.Template.Application.Abstractions.Logging;
using NetArchTest.Rules;

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
        // Arrange
        var rule = _types
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I");

        // Act
        var result = rule.GetResult();

        // Assert
        Assert.True(result.IsSuccessful, result.GetFailingTypes());
    }
}
