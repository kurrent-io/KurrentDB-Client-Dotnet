namespace KurrentDB.Client;
#pragma warning disable 1591

/// <summary>
/// Provides functionality for combining hash codes in a way that's compatible with .NET Framework 4.8.
/// </summary>
readonly struct HashCode {
	readonly int _value;

	HashCode(int value) => _value = value;

	/// <summary>
	/// Starting point for hash code generation.
	/// </summary>
	public static readonly HashCode Hash = new(17); // Prime number for better distribution

	/// <summary>
	/// Combines a value's hash code with the current hash.
	/// </summary>
	public HashCode Combine<T>(T value) {
		unchecked {
			return new HashCode((_value * 31) ^ (value?.GetHashCode() ?? 0));
		}
	}

	/// <summary>
	/// Combines a specific value with the hash.
	/// </summary>
	public HashCode Combine(int value) {
		unchecked {
			return new HashCode((_value * 31) ^ value);
		}
	}

	/// <summary>
	/// Combines a specific value with the hash.
	/// </summary>
	public HashCode Combine(string? value) {
		unchecked {
			return new HashCode((_value * 31) ^ (value?.GetHashCode() ?? 0));
		}
	}

	/// <summary>
	/// Combines enumerable values into the current hash.
	/// </summary>
	public HashCode CombineEnumerable<T>(IEnumerable<T>? values) {
		if (values is null)
			return this;

		var hash = this;
		return values.Aggregate(hash, (current, value) => current.Combine(value));
	}

	/// <summary>
	/// Combines multiple values into a single hash code.
	/// </summary>
	public static int Combine<T1, T2>(T1 value1, T2 value2) =>
		Hash.Combine(value1).Combine(value2);

	/// <summary>
	/// Combines multiple values into a single hash code.
	/// </summary>
	public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3) =>
		Hash.Combine(value1).Combine(value2).Combine(value3);

	/// <summary>
	/// Combines multiple values into a single hash code.
	/// </summary>
	public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4) =>
		Hash.Combine(value1).Combine(value2).Combine(value3).Combine(value4);

	/// <summary>
	/// Combines multiple values into a single hash code.
	/// </summary>
	public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5) =>
		Hash.Combine(value1).Combine(value2).Combine(value3).Combine(value4).Combine(value5);

	public static implicit operator int(HashCode value) =>
		value._value;
}
