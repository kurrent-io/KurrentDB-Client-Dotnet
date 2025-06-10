using System.Diagnostics;

namespace Kurrent.Client.Model;

[PublicAPI]
[DebuggerDisplay("{ToDebugString()}")]
public record StreamRevision : IComparable<StreamRevision>, IComparable {
	/// <summary>
	/// A special value that refers to an invalid, unassigned or default revision.
	/// </summary>
	public static readonly StreamRevision Unset = new StreamRevision((long)-1000);

	/// <summary>
	/// The beginning (i.e., the first event) of a stream.
	/// </summary>
	public static readonly StreamRevision Min = new((long)0);

	/// <summary>
	/// The end of a stream. Use this when reading a stream backwards, or subscribing live to a stream.
	/// </summary>
	public static readonly StreamRevision Max = new(long.MaxValue);

	StreamRevision(long value) => Value = value;

	public long Value { get; }

	public static StreamRevision From(long value) =>
		value switch {
			-1000 => Unset,
			>= 0  => new StreamRevision(value),
			_     => throw new InvalidStreamRevision(value)
		};

	public static implicit operator long(StreamRevision _)   => _.Value;
	public static implicit operator StreamRevision(long _)   => From(_);
    public static implicit operator ExpectedStreamState(StreamRevision _) => new(_.Value);

	public override string ToString() => Value.ToString();

	/// <summary>
	/// Creates a debug view.
	/// </summary>
	public string ToDebugString() {
		var valueText = Value switch {
			-1000         => "Unset",
			0             => "Min",
			long.MaxValue => "Max",
			_             => Value.ToString()
		};

		return $"StreamRevision: {valueText}";
	}

	#region . relational members .

	public int CompareTo(StreamRevision other) => Value.CompareTo(other.Value);

	public int CompareTo(object? obj) {
		if (ReferenceEquals(null, obj)) return 1;

		return obj is StreamRevision other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(StreamRevision)}");
	}

	public static bool operator <(StreamRevision left, StreamRevision right) => left.CompareTo(right) < 0;
	public static bool operator >(StreamRevision left, StreamRevision right) => left.CompareTo(right) > 0;
	public static bool operator <=(StreamRevision left, StreamRevision right) => left.CompareTo(right) <= 0;
	public static bool operator >=(StreamRevision left, StreamRevision right) => left.CompareTo(right) >= 0;

	#endregion
}

public class InvalidStreamRevision(long value) : Exception($"Stream revision is invalid: {value}");
