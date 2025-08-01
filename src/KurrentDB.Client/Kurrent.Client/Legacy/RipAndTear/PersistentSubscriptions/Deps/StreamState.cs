namespace KurrentDB.Client;

/// <summary>
/// A structure that represents the state the stream should be in when writing.
/// </summary>
readonly struct StreamState : IEquatable<StreamState> {
	/// <summary>
	/// The stream should not exist.
	/// </summary>
	public static readonly StreamState NoStream = new(Constants.NoStream);

	/// <summary>
	/// The stream may or may not exist.
	/// </summary>
	public static readonly StreamState Any = new(Constants.Any);

	/// <summary>
	/// The stream must exist.
	/// </summary>
	public static readonly StreamState StreamExists = new(Constants.StreamExists);

	public static StreamState StreamRevision(ulong value) => new((long)value);

	readonly long _value;

	static class Constants {
		public const int NoStream     = -1;
		public const int Any          = -2;
		public const int StreamExists = -4;
	}

	internal StreamState(long value) {
		switch (value) {
			case Constants.NoStream:
			case Constants.Any:
			case Constants.StreamExists:
				_value = value;
				return;

			default:
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_value = value;
				return;
		}
	}

	/// <inheritdoc />
	public bool Equals(StreamState other) => _value == other._value;

	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is StreamState other && Equals(other);

	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Hash.Combine(_value);

	/// <summary>
	/// Compares left and right for equality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>Returns True when left and right are equal.</returns>
	public static bool operator ==(StreamState left, StreamState right) => left.Equals(right);

	/// <summary>
	/// Compares left and right for inequality.
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns>Returns True when left and right are not equal.</returns>
	public static bool operator !=(StreamState left, StreamState right) => !left.Equals(right);

	/// <summary>
	/// Converts the <see cref="StreamState"/> to a <see cref="long"/>. It is not meant to be used directly from your code.
	/// </summary>
	/// <returns></returns>
	public long ToInt64() => _value;

	public bool HasPosition => _value >= 0;

	public static implicit operator StreamState(ulong value) => new((long)value);

	/// <inheritdoc />
	public override string ToString() => _value switch {
		Constants.NoStream     => nameof(NoStream),
		Constants.Any          => nameof(Any),
		Constants.StreamExists => nameof(StreamExists),
		_                      => _value.ToString()
	};
}
