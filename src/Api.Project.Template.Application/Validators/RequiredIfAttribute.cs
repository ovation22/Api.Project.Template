using System.ComponentModel.DataAnnotations;

namespace Api.Project.Template.Application.Validators;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class RequiredIfAttribute(string propertyName, object desiredValue) : ValidationAttribute
{
    private string PropertyName { get; } = propertyName;
    private object DesiredValue { get; } = desiredValue;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(PropertyName);

        if (property == null)
        {
            return new ValidationResult($"Property '{PropertyName}' not found.");
        }

        var propValue = property.GetValue(validationContext.ObjectInstance);

        if (propValue == null || !propValue.Equals(DesiredValue))
        {
            // Desired condition not met, skip validation
            return ValidationResult.Success;
        }

        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required.");
        }

        return ValidationResult.Success;
    }
}