using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Features;

/// <summary>
/// Represents a requirement for a feature with validation rules.
/// </summary>
public record FeatureRequirement {
    public static readonly FeatureRequirement None = new() {
        Name             = null!,
        Value            = false,
        EnforcementLevel = EnforcementLevel.Optional,
        Description      = null!
    };

    /// <summary>
    /// Unique name of the requirement.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Value of this requirement.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// How this requirement is enforced.
    /// </summary>
    public EnforcementLevel EnforcementLevel { get; init; } = EnforcementLevel.Optional;

    /// <summary>
    /// Human-readable description of the requirement.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Message shown when requirement is violated.
    /// </summary>
    public string ViolationMessage { get; init; } = "";

    /// <summary>
    /// Converts the requirement value to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <returns>The strongly-typed value.</returns>
    /// <exception cref="InvalidCastException">Thrown when the value cannot be converted to the requested type.</exception>
    /// <exception cref="FormatException">Thrown when the string representation cannot be parsed to the requested type.</exception>
    public T GetValue<T>() {
        if (Value is null)
            throw new InvalidCastException($"Cannot convert null value to {typeof(T).Name} for requirement '{Name}'");

        try {
            if (Value is T typedValue)
                return typedValue;

            if (typeof(T).IsEnum && Value is string enumStr)
                return (T)Enum.Parse(typeof(T), enumStr, true);

            if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
                return (T)Convert.ChangeType(Value, typeof(T));

            throw new InvalidCastException($"Cannot convert {Value.GetType().Name} to {typeof(T).Name} for requirement '{Name}'");
        }
        catch (Exception ex) when (ex is not InvalidCastException && ex is not FormatException) {
            throw new InvalidCastException($"Failed to convert value to {typeof(T).Name} for requirement '{Name}'", ex);
        }
    }

    /// <summary>
    /// Attempts to get the value of this requirement as a specific type.
    /// </summary>
    /// <param name="value">
    /// The output parameter that will contain the value if conversion is successful; otherwise, it will be set to default.
    /// </param>
    /// <typeparam name="T">
    /// The type to which the value should be converted. This type must be compatible with the value's type.
    /// </typeparam>
    /// <returns>
    /// True if the value was successfully converted to the specified type; otherwise, false.
    /// </returns>
    public bool TryGetValue<T>([MaybeNullWhen(false)] out T value) {
        try {
            value = GetValue<T>();
            return true;
        }
        catch (InvalidCastException) {
            value = default;
            return false;
        }
        catch (FormatException) {
            value = default;
            return false;
        }
    }
}
