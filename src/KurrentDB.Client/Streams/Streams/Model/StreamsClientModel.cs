using JetBrains.Annotations;

namespace KurrentDB.Client;

/// <summary>
/// Represents a request to append events to a specific stream in KurrentDB.
/// </summary>
/// <param name="Stream">
/// The name of the stream to which the events are to be appended.
/// </param>
/// <param name="ExpectedState">
/// The expected state of the stream before performing the append operation
/// to enforce optimistic concurrency control by ensuring that the
/// stream's actual state matches the expected state before appending.
/// </param>
/// <param name="Messages">
/// A collection of <see cref="EventData"/> representing the events being appended
/// to the stream. Each event can contain a payload and an associated metadata.
/// </param>
[PublicAPI]
public record AppendStreamRequest(string Stream, StreamState ExpectedState, IEnumerable<EventData> Messages);

/// <summary>
/// Represents the outcome of an append operation.
/// </summary>
/// <param name="Stream">
/// The name of the stream to which records were appended.
/// </param>
/// <param name="StreamRevision">
/// The expected revision of the stream after the append operation.
/// </param>
[PublicAPI]
public record AppendResponse(string Stream, long StreamRevision);

[PublicAPI]
public readonly struct MultiStreamAppendResponse(long position, IEnumerable<AppendResponse>? responses = null) {
	/// <summary>
	/// The position of the last appended record in the transaction.
	/// </summary>
	public readonly long Position = position;

	/// <summary>
	/// The collection of responses for each stream appended in the transaction.
	/// </summary>
	public readonly IEnumerable<AppendResponse>? Responses = responses;
}
