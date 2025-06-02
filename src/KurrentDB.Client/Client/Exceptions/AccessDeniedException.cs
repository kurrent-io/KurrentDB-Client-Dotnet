namespace KurrentDB.Client;

/// <summary>
/// Exception thrown when a user is not authorised to carry out
/// an operation.
/// </summary>
public class AccessDeniedException : Exception {
	/// <summary>
	/// Constructs a new <see cref="AccessDeniedException" />.
	/// </summary>
	public AccessDeniedException(string message, Exception? innerException = null) : base(message, innerException) { }

	/// <summary>
	/// Constructs a new <see cref="AccessDeniedException" />.
	/// </summary>
	public AccessDeniedException() : base("Access denied.") { }
}
