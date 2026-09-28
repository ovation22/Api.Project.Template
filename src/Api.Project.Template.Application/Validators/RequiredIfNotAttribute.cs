using System.ComponentModel.DataAnnotations;

namespace Api.Project.Template.Application.Validators;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RequiredIfNotAttribute(string otherProperty, object valueToCheck) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(otherProperty);

        if (property == null)
        {
            return new ValidationResult($"Unknown property {otherProperty}");
        }

        var otherPropertyValue = property.GetValue(validationContext.ObjectInstance);

        if (otherPropertyValue != null && !otherPropertyValue.Equals(valueToCheck))
        {
            // If the other property value is not equal to the specified value,
            // then the current property is required (blank strings count as missing, as in RequiredIf)
            return value == null || string.IsNullOrWhiteSpace(value.ToString())
                ? new ValidationResult(
                    ErrorMessage ?? $"{validationContext.DisplayName} is required.",
                    validationContext.MemberName is { } memberName ? [memberName] : null)
                : ValidationResult.Success;
        }

        // If the other property value is equal to the specified value,
        // then the current property is not required
        return ValidationResult.Success;
    }
}