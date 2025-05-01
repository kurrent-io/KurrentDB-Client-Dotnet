namespace KurrentDB.Client;

/// <summary>
/// A class representing the options to apply to an individual operation.
/// </summary>
public class KurrentDBClientOperationOptions {
	/// <summary>
	/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
	/// </summary>
	public bool ThrowOnAppendFailure { get; set; }

	/// <summary>
	/// The batch size, in bytes.
	/// </summary>
	public int BatchAppendSize { get; set; }

	/// <summary>
	/// A callback function to extract the authorize header value from the <see cref="UserCredentials"/> used in the operation.
	/// </summary>
	public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; set; } = null!;

	/// <summary>
	/// The default <see cref="KurrentDBClientOperationOptions"/>.
	/// </summary>
	public static KurrentDBClientOperationOptions Default => new() {
		ThrowOnAppendFailure         = true,
		GetAuthenticationHeaderValue = (userCredentials, _) => new(userCredentials.ToString()),
		BatchAppendSize              = 3 * 1024 * 1024 // 3MB
	};


	/// <summary>
	/// Clones a copy of the current <see cref="KurrentDBClientOperationOptions"/>.
	/// </summary>
	/// <returns></returns>
	public KurrentDBClientOperationOptions Clone() => new() {
		ThrowOnAppendFailure         = ThrowOnAppendFailure,
		GetAuthenticationHeaderValue = GetAuthenticationHeaderValue,
		BatchAppendSize              = BatchAppendSize
	};
}
