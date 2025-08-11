namespace KurrentDB.Client;

#pragma warning disable CS8509
/// <summary>
/// Exception thrown when a transaction exceeds the allowed maximum size limit.
/// </summary>
public class TransactionMaxSizeExceededException : Exception {
	/// <summary>
	/// The maximum size, in bytes, allowed for a transaction before it is considered invalid.
	/// </summary>
	public readonly long MaxSize;

	public TransactionMaxSizeExceededException(long maxSize, Exception? exception = null)
		: base($"Transaction size exceeded the maximum allowed size of {maxSize} bytes.", exception) {
		MaxSize = maxSize;
	}
}
