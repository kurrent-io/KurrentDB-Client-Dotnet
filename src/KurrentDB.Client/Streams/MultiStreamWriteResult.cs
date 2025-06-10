using JetBrains.Annotations;
using KurrentDB.Protocol.V2;

namespace KurrentDB.Client;

[PublicAPI]
public class MultiStreamWriteResult {
	readonly List<AppendStreamSuccess>? _successes;
	public List<AppendStreamFailure> Failures { get; } = [];

	internal MultiStreamWriteResult(MultiStreamAppendResponse response) {
		if (response.ResultCase == MultiStreamAppendResponse.ResultOneofCase.Success) {
			_successes = new List<AppendStreamSuccess>(response.Success.Output.Count);

			foreach (var success in response.Success.Output)
				_successes.Add(
					new AppendStreamSuccess(success.Stream, success.Position, new StreamState(success.StreamRevision))
				);
		} else {
			_successes = null;
			Failures = new List<AppendStreamFailure>(response.Failure.Output.Count);

			foreach (var failure in response.Failure.Output)
				Failures.Add(new AppendStreamFailure(failure));
		}
	}

	public bool TryGetSuccesses(out List<AppendStreamSuccess> successes) {
		successes = [];

		if (_successes == null)
			return false;

		successes = _successes;
		return true;

	}
}
