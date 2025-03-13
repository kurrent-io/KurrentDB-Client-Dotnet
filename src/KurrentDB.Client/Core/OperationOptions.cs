namespace KurrentDB.Client;

/// <summary>
/// A class representing the options to apply to an individual operation.
/// </summary>
public record OperationOptions {
	/// <summary>
	/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
	/// </summary>
	public bool? ThrowOnAppendFailure { get; set; }

	/// <summary>
	/// The batch size, in bytes.
	/// </summary>
	public int? BatchAppendSize { get; set; }

	/// <summary>
	/// Maximum time that the operation will be run
	/// </summary>
	public TimeSpan? Deadline { get; set; }

	/// <summary>
	/// The <see cref="UserCredentials"/> for the operation.
	/// </summary>
	public UserCredentials? UserCredentials { get; set; }

	/// <summary>
	/// Clones a copy of the current <see cref="KurrentDBClientOperationOptions"/>.
	/// </summary>
	/// <returns></returns>
	public OperationOptions With(KurrentDBClientOperationOptions clientOperationOptions) =>
		new() {
			ThrowOnAppendFailure = ThrowOnAppendFailure ?? clientOperationOptions.ThrowOnAppendFailure,
			BatchAppendSize      = BatchAppendSize ?? clientOperationOptions.BatchAppendSize
		};
}
