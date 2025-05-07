namespace KurrentDB.Client;

/// <summary>
/// Exception thrown if the expected version specified on an operation
/// does not match the version of the stream when the operation was attempted.
/// </summary>
public class WrongExpectedVersionException : Exception {
	/// <summary>
	/// Constructs a new instance of <see cref="WrongExpectedVersionException" /> with the expected and actual versions if available.
	/// </summary>
	public WrongExpectedVersionException(string stream, StreamState expectedState, StreamState actualState, Exception? exception = null, string? message = null)
		: base(message ?? $"Append failed due to WrongExpectedVersion. Stream: {stream}, Expected version: {expectedState}, Actual version: {actualState}", exception) {
		StreamName          = stream;
		ActualStreamState   = actualState;
		ExpectedStreamState = expectedState;
		ExpectedVersion     = expectedState.ToInt64();
		ActualVersion       = actualState.ToInt64();
	}

	/// <summary>
	/// The stream identifier.
	/// </summary>
	public string StreamName { get; }

	/// <summary>
	/// If available, the expected version specified for the operation that failed.
	/// </summary>
	public long? ExpectedVersion { get; }

	/// <summary>
	/// If available, the current version of the stream that the operation was attempted on.
	/// </summary>
	public long? ActualVersion { get; }

	/// <summary>
	/// The current <see cref="StreamRevision" /> of the stream that the operation was attempted on.
	/// </summary>
	public StreamState ActualStreamState { get; }

	/// <summary>
	/// If available, the expected version specified for the operation that failed.
	/// </summary>
	public StreamState ExpectedStreamState { get; }
}
