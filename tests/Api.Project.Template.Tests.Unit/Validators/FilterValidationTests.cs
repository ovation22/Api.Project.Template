using System.ComponentModel.DataAnnotations;
using Api.Project.Template.Application.Common.Pagination;

namespace Api.Project.Template.Tests.Unit.Validators;

/// <summary>
/// RequiredIf / RequiredIfNot as applied to <see cref="Filter"/>: Value is required unless the operator is
/// Between; ValueFrom and ValueTo are required only for Between. Blank strings count as missing.
/// </summary>
[Trait("Category", "Validators")]
public class FilterValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Value_MissingOrBlank_WithNonBetweenOperator_IsInvalid(string? value)
    {
        // Arrange
        var filter = new Filter { Operator = FilterOperator.Eq, Value = value };

        // Act
        var errors = Validate(filter);

        // Assert
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Filter.Value)));
    }

    [Fact]
    public void Value_Present_WithNonBetweenOperator_IsValid()
    {
        // Arrange
        var filter = new Filter { Operator = FilterOperator.Eq, Value = "20" };

        // Act
        var errors = Validate(filter);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null, "10")]
    [InlineData("  ", "10")]
    [InlineData("1", null)]
    public void Range_MissingOrBlank_WithBetweenOperator_IsInvalid(string? from, string? to)
    {
        // Arrange
        var filter = new Filter { Operator = FilterOperator.Between, ValueFrom = from, ValueTo = to };

        // Act
        var errors = Validate(filter);

        // Assert
        Assert.NotEmpty(errors);
        Assert.All(errors, e => Assert.Subset(
            new HashSet<string> { nameof(Filter.ValueFrom), nameof(Filter.ValueTo) }, e.MemberNames.ToHashSet()));
    }

    [Fact]
    public void Range_Present_WithBetweenOperator_IsValidWithoutValue()
    {
        // Arrange
        var filter = new Filter { Operator = FilterOperator.Between, ValueFrom = "1", ValueTo = "10" };

        // Act
        var errors = Validate(filter);

        // Assert
        Assert.Empty(errors);
    }

    private static List<ValidationResult> Validate(Filter filter)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(filter, new ValidationContext(filter), results, validateAllProperties: true);
        return results;
    }
}
