namespace Kurrent.Client.Registry;

/// <summary>
/// Represents a violation of a field in a request, typically used to indicate validation errors.
/// </summary>
/// <param name="Field">
/// The name of the field that violated validation rules.
/// </param>
/// <param name="Description">
/// A description of the violation, providing details about why the field is invalid or what rule was violated.
/// </param>
public record FieldViolation(string Field, string Description);
