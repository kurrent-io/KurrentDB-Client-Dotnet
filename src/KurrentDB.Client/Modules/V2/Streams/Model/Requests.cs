using JetBrains.Annotations;
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

[PublicAPI]
public record AppendStreamRequest {
	public string        Stream           { get; set; } = "";
	public List<Message> Messages         { get; set; } = [];
	public long          ExpectedRevision { get; set; }
}

[PublicAPI]
[GenerateOneOf]
public partial class AppendStreamResult : OneOfBase<AppendStreamSuccess, AppendStreamFailure>;

[PublicAPI]
[GenerateOneOf]
public partial class MultiStreamAppendResult : OneOfBase<AppendStreamSuccesses, AppendStreamFailures>;
