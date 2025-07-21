namespace KurrentDB.Client;

/// <summary>
/// Indicates the preferred KurrentDB node type to connect to.
/// </summary>
public enum NodePreference {
	/// <summary>
	/// When attempting connection, prefers leader node.
	/// </summary>
	Leader = 0,

	/// <summary>
	/// When attempting connection, prefers follower node.
	/// </summary>
	Follower = 1,

	/// <summary>
	/// When attempting connection, has no node preference.
	/// </summary>
	Random = 2,

	/// <summary>
	/// When attempting connection, prefers read only replicas.
	/// </summary>
	ReadOnlyReplica = 3
}
