using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace Api.Project.Template.Tests.Architecture;

internal static class TestResultExtensions
{
    public static string GetFailingTypes(this TestResult result)
    {
        return result.FailingTypeNames != null ? string.Join(", ", result.FailingTypeNames) : string.Empty;
    }
}
