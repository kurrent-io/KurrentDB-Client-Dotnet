using Kurrent.Variant;

namespace Kurrent.Client.Operations;

#region admin

[PublicAPI]
public readonly partial record struct ShutdownError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct MergeIndexesError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct ResignNodeError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct SetNodePriorityError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct RestartPersistentSubscriptionsError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

#endregion

#region scavenge

/// <summary>
/// A structure representing the result of a scavenge operation.
/// </summary>
public readonly record struct DatabaseScavengeResult {
	/// <summary>
	/// The ID of the scavenge operation.
	/// </summary>
	public string ScavengeId { get; }

	/// <summary>
	/// The <see cref="ScavengeStatus"/> of the scavenge operation.
	/// </summary>
	public ScavengeStatus Status { get; }

	/// <summary>
	/// A scavenge operation that has started.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Started(string scavengeId) =>
		new DatabaseScavengeResult(scavengeId, ScavengeStatus.Started);

	/// <summary>
	/// A scavenge operation that has stopped.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Stopped(string scavengeId) =>
		new DatabaseScavengeResult(scavengeId, ScavengeStatus.Stopped);

	/// <summary>
	/// A scavenge operation that is currently in progress.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult InProgress(string scavengeId) =>
		new DatabaseScavengeResult(scavengeId, ScavengeStatus.InProgress);

	/// <summary>
	/// A scavenge operation whose state is unknown.
	/// </summary>
	/// <param name="scavengeId"></param>
	/// <returns></returns>
	public static DatabaseScavengeResult Unknown(string scavengeId) =>
		new DatabaseScavengeResult(scavengeId, ScavengeStatus.Unknown);

	DatabaseScavengeResult(string scavengeId, ScavengeStatus status) {
		ScavengeId = scavengeId;
		Status     = status;
	}
}

/// <summary>
/// An enumeration that represents the result of a scavenge operation.
/// </summary>
public enum ScavengeStatus {
	/// <summary>
	/// The scavenge operation has started.
	/// </summary>
	Started,
	/// <summary>
	/// The scavenge operation is in progress.
	/// </summary>
	InProgress,

	/// <summary>
	/// The scavenge operation has stopped.
	/// </summary>
	Stopped,

	/// <summary>
	/// The status of the scavenge operation was unknown.
	/// </summary>
	Unknown
}


[PublicAPI]
public readonly partial record struct StartScavengeError : IVariantResultError<
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

[PublicAPI]
public readonly partial record struct StopScavengeError : IVariantResultError<
	ErrorDetails.ScavengeNotFound,
	ErrorDetails.AccessDenied,
	ErrorDetails.NotAuthenticated
>;

#endregion
