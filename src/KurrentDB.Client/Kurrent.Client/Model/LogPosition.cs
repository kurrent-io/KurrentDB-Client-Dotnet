// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable DuplicatedSequentialIfBodies

using System.Text.RegularExpressions;

namespace Kurrent.Client.Model;

/// <summary>
/// Record position in the global log.
/// </summary>
[PublicAPI]
public readonly partial record struct LogPosition : IComparable<LogPosition>, IComparable {
	/// <summary>
	/// Record position in the global log.
	/// </summary>
	public LogPosition(long value) => Value = value;

	/// <summary>
    /// Indicates that the log position is not set and therefore is invalid and should be ignored.
    /// Represented by a value of -1000.
    /// </summary>
    public static readonly LogPosition Unset = new(-1000);

    /// <summary>
	/// Initial position in the transaction file that is used when no other position is specified.
	/// Represented by a value of 0.
	/// </summary>
	public static readonly LogPosition Earliest = new(0);

	/// <summary>
	/// Initial position in the transaction file that represents the end of the transaction file.
	/// Represented by a value of long.MaxValue.
	/// </summary>
	public static readonly LogPosition Latest = new(long.MaxValue);

	/// <summary>
	/// Represents the underlying value of the log position in the global transaction log.
	/// </summary>
	public long Value { get; init; }

	/// <summary>
	/// Creates a LogPosition instance from a provided position value.
	/// </summary>
	/// <param name="position">The position value from which to create the LogPosition. Must be Unset, greater than or equal to 0, or an exception will be thrown.</param>
	/// <returns>A new instance of LogPosition corresponding to the provided position value.</returns>
	/// <exception cref="InvalidLogPosition">Thrown when the provided position value is less than 0 and not equal to Unset.</exception>
	public static LogPosition From(long position) =>
		position switch {
			-1000 => Unset,
			>= 0  => new LogPosition(position),
			_     => throw new InvalidLogPosition(new LogPosition(position))
		};

	/// <summary>
	/// Creates a <see cref="LogPosition"/> instance from a provided position string.
	/// </summary>
	/// <param name="position">
	/// The position string in the format "C:commit/P:prepare".
	/// </param>
	/// <returns>
	/// A new instance of <see cref="LogPosition"/> corresponding to the provided position string.
	/// </returns>
	/// <exception cref="InvalidLogPosition">
	/// Thrown when the provided string is not in the correct format or contains invalid values.
	/// </exception>
	public static LogPosition From(string position) {
		if (string.IsNullOrEmpty(position))
			return Unset;

		var match = LogPositionRegex().Match(position.Trim());

		if (!match.Success)
			throw new InvalidLogPosition(position);

		if (!ulong.TryParse(match.Groups["commit"].Value, out var commitPosition))
			throw new InvalidLogPosition(position);

		return new LogPosition((long)commitPosition);
	}

	public static implicit operator long(LogPosition _)  => _.Value;
	public static implicit operator ulong(LogPosition _) => (ulong)_.Value;

	public static implicit operator LogPosition(long _)   => From(_);

    public override string ToString() => Value.ToString();

    #region . relational members .

    public int CompareTo(LogPosition other) =>
	    Value.CompareTo(other.Value);

    public int CompareTo(object? obj) =>
	    obj switch {
		    null              => 1,
		    LogPosition other => CompareTo(other),
		    _                 => throw new ArgumentException($"Object must be of type {nameof(LogPosition)}")
	    };

    public static bool operator <(LogPosition left, LogPosition right) => left.CompareTo(right) < 0;
    public static bool operator >(LogPosition left, LogPosition right) => left.CompareTo(right) > 0;
    public static bool operator <=(LogPosition left, LogPosition right) => left.CompareTo(right) <= 0;
    public static bool operator >=(LogPosition left, LogPosition right) => left.CompareTo(right) >= 0;

	#endregion

	[GeneratedRegex(@"^C:(?<commit>[0-9]*)\/P:(?<prepare>[0-9]*)$")]
	private static partial Regex LogPositionRegex();
}

/// <summary>
/// Exception thrown when a log position is invalid.
/// </summary>
public class InvalidLogPosition : ArgumentException {
    /// <summary>
    /// Initializes a new instance with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The invalid log position.</param>
    public InvalidLogPosition(LogPosition value)
        : base($"Log position is invalid: {value.ToString()}") { }

    /// <summary>
    /// Initializes a new instance with the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The invalid log position as a string.</param>
    public InvalidLogPosition(string value)
        : base($"Log position is invalid: {value}") { }
}
