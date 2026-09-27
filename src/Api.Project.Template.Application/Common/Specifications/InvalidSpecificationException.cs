namespace Api.Project.Template.Application.Common.Specifications;

/// <summary>
/// Thrown when client-supplied sorting or filtering cannot be applied to the entity:
/// an unknown or computed property, a value that can't be converted to the property's type,
/// or an operator that isn't valid for the property's type.
/// Handlers translate it into an invalid result (HTTP 400) instead of a server error.
/// </summary>
public sealed class InvalidSpecificationException(string propertyName, string message) : Exception(message)
{
    /// <summary>
    /// The property name as supplied by the caller (before any property mapping).
    /// </summary>
    public string PropertyName { get; } = propertyName;
}
