namespace Kurrent;

/// <summary>
/// Represents a void type, since 'void' cannot be used as a generic type parameter.
/// This is a singleton type and has only one value.
/// </summary>
[PublicAPI]
public readonly record struct Unit {
    /// <summary>
    /// The single, default instance of the <see cref="Unit"/> type.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// Returns the string representation of the <see cref="Unit"/> type.
    /// </summary>
    /// <returns>A string representation of the unit type: "()".</returns>
    public override string ToString() => "()";
}
