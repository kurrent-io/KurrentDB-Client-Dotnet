namespace Kurrent.Client.Features;

/// <summary>
/// Represents server information including metadata and available features.
/// </summary>
public record ServerInfo {
	// /// <summary>
	// /// Unique identifier for this server node.
	// /// </summary>
	// public string NodeId { get; init; } = Guid.Empty.ToString();

	/// <summary>
	/// Semantic version of the server.
	/// </summary>
	public string Version { get; init; } = "0.0.0";

	// /// <summary>
	// /// Minimum client version required to connect.
	// /// </summary>
	// public string MinCompatibleClientVersion { get; init; } = "0.0.0";

	/// <summary>
	/// Features available on the server, with their enablement status.
	/// </summary>
	public ServerFeatures Features { get; init; } = new();
}
