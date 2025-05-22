using OneOf;

namespace KurrentDB.Client.Model;

[PublicAPI]
public record AppendStreamSuccess {
	public string Stream         { get; init; } = "";
	public long   Position       { get; init; }
	public long   StreamRevision { get; init; }
}

[PublicAPI]
public record AppendStreamFailure {
	public string    Stream { get; init; } = "";
	public Exception Error  { get; init; } = null!;
}

[PublicAPI]
public class AppendStreamSuccesses : List<AppendStreamSuccess> {
	public AppendStreamSuccesses() { }
	public AppendStreamSuccesses(IEnumerable<AppendStreamSuccess> input) : base(input) { }
}

[PublicAPI]
public class AppendStreamFailures : List<AppendStreamFailure> {
	public AppendStreamFailures() { }
	public AppendStreamFailures(IEnumerable<AppendStreamFailure> input) : base(input) { }
}

// [PublicAPI]
// public record AppendStreamRequest() {
// 	public AppendStreamRequest(string stream, StreamState expectedState, IEnumerable<Message> messages) : this() {
// 		Stream        = stream;
// 		ExpectedState = expectedState;
// 		Messages      = messages;
// 	}
//
// 	public string               Stream        { get; init; } = "";
// 	public IEnumerable<Message> Messages      { get; init; } = [];
// 	public StreamState          ExpectedState { get; init; } = StreamState.Any;
// }

[PublicAPI]
public record AppendStreamRequest() {
	public AppendStreamRequest(string stream, StreamState expectedState, IAsyncEnumerable<Message> messages) : this() {
		Stream        = stream;
		ExpectedState = expectedState;
		Messages      = messages;
	}

	public string                    Stream        { get; init; } = "";
	public StreamState               ExpectedState { get; init; } = StreamState.Any;
	public IAsyncEnumerable<Message> Messages      { get; init; } = AsyncEnumerable.Empty<Message>();
}


[PublicAPI]
[GenerateOneOf]
public partial class AppendStreamResult : OneOfBase<AppendStreamSuccess, AppendStreamFailure>;

[PublicAPI]
public record MultiStreamAppendRequest {
	public List<AppendStreamRequest> Requests { get; init; } = [];
}

[PublicAPI]
[GenerateOneOf]
public partial class MultiStreamAppendResult : OneOfBase<AppendStreamSuccesses, AppendStreamFailures>;
