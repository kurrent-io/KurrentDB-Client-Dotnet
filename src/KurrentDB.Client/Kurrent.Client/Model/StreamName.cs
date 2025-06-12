namespace Kurrent.Client.Model;

/// <summary>
/// Represents the name of a stream in KurrentDB.
/// </summary>
[PublicAPI]
public readonly record struct StreamName {
    StreamName(string value) => Value = value;

    /// <summary>
    /// Represents an uninitialized or empty stream name.
    /// </summary>
    public static readonly StreamName None = new("");

    /// <summary>
    /// The underlying string value of the stream name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Returns the string representation of the stream name.
    /// </summary>
    /// <returns>The string value of the stream name.</returns>
    public override string ToString() => Value;

    /// <summary>
    /// Creates a <see cref="StreamName"/> from a string value.
    /// </summary>
    /// <param name="value">The string value to create the stream name from. Cannot be null, empty, or whitespace.</param>
    /// <returns>A new <see cref="StreamName"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
    public static StreamName From(string value) {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Stream '{value}' is not valid.", nameof(value))
            : new(value);
    }

    /// <summary>
    /// Implicitly converts a string to a <see cref="StreamName"/>.
    /// </summary>
    /// <param name="value">The string to convert. Cannot be null, empty, or whitespace.</param>
    /// <returns>A new <see cref="StreamName"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
    public static implicit operator StreamName(string value)  => From(value);

    /// <summary>
    /// Implicitly converts a <see cref="StreamName"/> to its string representation.
    /// </summary>
    /// <param name="streamName">The <see cref="StreamName"/> to convert.</param>
    /// <returns>The string value of the stream name.</returns>
    public static implicit operator string(StreamName streamName) => streamName.ToString();
}
