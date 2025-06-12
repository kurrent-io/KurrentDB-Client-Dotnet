namespace Kurrent.Client.Model;

/// <summary>
/// Represents the expected state of a stream when performing operations such as appending messages.
/// </summary>
public readonly record struct ExpectedStreamState {
    /// <summary>
	/// The stream should not exist.
	/// </summary>
	public static readonly ExpectedStreamState NoStream = new(-1);

	/// <summary>
	/// The stream may or may not exist.
	/// </summary>
	public static readonly ExpectedStreamState Any = new(-2);

	/// <summary>
	/// The stream must exist.
	/// </summary>
	public static readonly ExpectedStreamState StreamExists = new(-4);

    public long Value { get; }

    internal ExpectedStreamState(long value) {
        Value = value switch {
            -1 or -2 or -4 or >= 0 => value,
            _                      => throw new ArgumentOutOfRangeException(
                nameof(value), value, "ExpectedStreamState must be NoStream(-1), Any(-2), StreamExists(-4), or a StreamRevision(0+)")
        };
	}

    public static implicit operator ExpectedStreamState(long _)           => new(_);
    public static implicit operator long(ExpectedStreamState _)           => _.Value;
    public static implicit operator StreamRevision(ExpectedStreamState _) => _.Value;
    public static implicit operator ulong(ExpectedStreamState _)          => (ulong)_.Value;
}
