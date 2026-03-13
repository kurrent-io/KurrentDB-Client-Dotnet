using JetBrains.Annotations;

namespace KurrentDB.Client;

/// <summary>
/// Represents a record to be appended to a specific stream in an <see cref="KurrentDBClient.AppendRecordsAsync"/> operation.
/// Each record specifies its own target stream, allowing interleaved writes across multiple streams.
/// </summary>
/// <param name="Stream">The name of the target stream for this record.</param>
/// <param name="Record">The event data to append.</param>
[PublicAPI]
public record AppendRecord(string Stream, EventData Record);

/// <summary>
/// Represents a consistency check to be evaluated before committing an <see cref="KurrentDBClient.AppendRecordsAsync"/> operation.
/// Checks are decoupled from writes: a check can reference any stream, whether or not the request writes to it.
/// </summary>
[PublicAPI]
public abstract record ConsistencyCheck {
	/// <summary>
	/// A check that asserts a stream is at a specific revision or lifecycle state before commit.
	/// </summary>
	/// <param name="Stream">The stream name to check.</param>
	/// <param name="ExpectedState">The expected state of the stream (revision number or state constant).</param>
	[PublicAPI]
	public record StreamStateCheck(string Stream, StreamState ExpectedState) : ConsistencyCheck;
}
