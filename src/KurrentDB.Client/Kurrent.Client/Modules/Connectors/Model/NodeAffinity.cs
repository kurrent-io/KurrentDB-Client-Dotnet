namespace Kurrent.Client.Connectors;

public enum NodeAffinity {
    Any = 0,

    /// <summary>
    /// The connector prefers to read from the leader node.
    /// </summary>
    Leader = 1,

    /// <summary>
    /// The connector prefers to read from a follower node.
    /// </summary>
    Follower = 2,

    /// <summary>
    /// The connector prefers to read from a read-only replica node.
    /// </summary>
    ReadonlyReplica = 3,
}
