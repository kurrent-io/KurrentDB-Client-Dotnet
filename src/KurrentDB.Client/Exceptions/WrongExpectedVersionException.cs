using System;

namespace KurrentDB.Client;

/// <summary>
/// Exception thrown if the expected version specified on an operation
/// does not match the version of the stream when the operation was attempted.
/// </summary>
public class WrongExpectedVersionException : Exception {
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

	/// <summary>
	/// Constructs a new instance of <see cref="WrongExpectedVersionException" /> with the expected and actual versions if available.
	/// </summary>
	public WrongExpectedVersionException(string streamName, StreamState expectedStreamState,
	                                     StreamState actualStreamState, Exception? exception = null, string? message = null) :
		base(
			message ?? $"Append failed due to WrongExpectedVersion. Stream: {streamName}, Expected version: {expectedStreamState}, Actual version: {actualStreamState}",
			exception) {
		StreamName          = streamName;
		ActualStreamState   = actualStreamState;
		ExpectedStreamState = expectedStreamState;
		ExpectedVersion     = expectedStreamState.ToInt64();
		ActualVersion       = actualStreamState.ToInt64();
	}
}