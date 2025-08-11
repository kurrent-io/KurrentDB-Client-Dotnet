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
/// Represents the successful outcome of an append operation to a specific stream in the system.
/// </summary>
/// <param name="Stream">
/// The name of the stream where the events have been successfully appended.
/// </param>
/// <param name="Position">
/// The position in the stream after the append operation, indicating where the event(s) were written.
/// </param>
[PublicAPI]
public record AppendStreamSuccess(string Stream, long Position);

/// <summary>
/// Represents the result of a multi-stream append operation in KurrentDB.
/// </summary>
/// <seealso cref="MultiAppendSuccess"/>
/// <seealso cref="MultiAppendFailure"/>
[PublicAPI]
public abstract class MultiAppendWriteResult {
	public abstract bool IsSuccess { get; }
	public          bool IsFailure => !IsSuccess;
}

[PublicAPI]
public sealed class MultiAppendSuccess : MultiAppendWriteResult {
	public override bool                  IsSuccess => true;
	public          AppendStreamSuccesses Successes { get; }

	internal MultiAppendSuccess(AppendStreamSuccesses successes) =>
		Successes = successes;
}

[PublicAPI]
public sealed class MultiAppendFailure : MultiAppendWriteResult {
	public override bool                 IsSuccess => false;
	public          AppendStreamFailures Failures  { get; }

	internal MultiAppendFailure(AppendStreamFailures failures) =>
		Failures = failures;
}

[PublicAPI]
public class AppendStreamSuccesses : List<AppendStreamSuccess> {
	public AppendStreamSuccesses() { }
	public AppendStreamSuccesses(IEnumerable<AppendStreamSuccess> input) : base(input) { }
}

[PublicAPI]
public class AppendStreamFailures : List<Exception> {
	public AppendStreamFailures() { }
	public AppendStreamFailures(IEnumerable<Exception> input) : base(input) { }
}
