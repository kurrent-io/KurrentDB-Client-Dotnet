namespace Kurrent;

/// <summary>
/// Represents a void type, since 'void' cannot be used as a generic type parameter.
/// This is a singleton type and has only one value.
/// </summary>
[PublicAPI]
public readonly record struct Void {
    /// <summary>
    /// The single, default instance of the <see cref="Void"/> type.
    /// </summary>
    public static readonly Void Value;

    /// <summary>
    /// Returns the string representation of the <see cref="Void"/> type.
    /// </summary>
    /// <returns>A string representation of the unit type: "()".</returns>
    public override string ToString() => "()";
}
