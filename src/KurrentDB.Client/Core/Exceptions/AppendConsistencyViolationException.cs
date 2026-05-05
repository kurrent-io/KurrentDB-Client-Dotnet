using Grpc.Core;
using KurrentDB.Protocol.V2.Streams.Errors;

namespace KurrentDB.Client;

/// <summary>
/// Exception thrown when one or more consistency checks fail during an AppendRecords operation.
/// The entire transaction is aborted and no records are written.
/// </summary>
public class AppendConsistencyViolationException : Exception {
	/// <summary>
	/// The consistency violations that caused the transaction to be aborted.
	/// </summary>
	public IReadOnlyList<Violation> Violations { get; }

	/// <summary>
	/// Constructs a new <see cref="AppendConsistencyViolationException"/>.
	/// </summary>
	public AppendConsistencyViolationException(IReadOnlyList<Violation> violations, Exception? innerException = null)
		: base(FormatMessage(violations), innerException) {
		Violations = violations;
	}

	public static AppendConsistencyViolationException FromRpcException(RpcException ex) => FromRpcStatus(ex.GetRpcStatus()!);

	public static AppendConsistencyViolationException FromRpcStatus(Google.Rpc.Status status) {
		var details = status.GetDetail<AppendConsistencyViolationErrorDetails>();
		var violations = details.Violations.Select(v => {
			if (v.StreamState != null) {
				return new Violation(
					v.CheckIndex,
					v.StreamState.Stream,
					new StreamState(v.StreamState.ExpectedState),
					new StreamState(v.StreamState.ActualState)
				);
			}

			return new Violation(v.CheckIndex, string.Empty, default, default);
		}).ToList();

		return new AppendConsistencyViolationException(violations);
	}

	static string FormatMessage(IReadOnlyList<Violation> violations) {
		var details = string.Join(", ", violations.Select(v =>
			$"[Check {v.CheckIndex}: Stream '{v.Stream}' expected state {v.ExpectedState}, actual state {v.ActualState}]"
		));
		return $"Append failed due to consistency violation(s): {details}";
	}

	/// <summary>
	/// Represents a single consistency check violation.
	/// </summary>
	/// <param name="CheckIndex">Index of the check in the original consistency checks list.</param>
	/// <param name="Stream">The name of the stream whose state was checked.</param>
	/// <param name="ExpectedState">The expected state of the stream.</param>
	/// <param name="ActualState">The actual state of the stream at the time the check was evaluated.</param>
	public record Violation(int CheckIndex, string Stream, StreamState ExpectedState, StreamState ActualState);
}
