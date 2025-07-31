namespace Kurrent.Client;

/// <summary>
/// Provides the details of a persistent subscription connection.
/// </summary>
public record PersistentSubscriptionConnectionInfo {
	/// <summary>
	/// Origin of this connection.
	/// </summary>
	public required string From { get; init; }

	/// <summary>
	/// Connection username.
	/// </summary>
	public required string Username { get; init; }

	/// <summary>
	/// The name of the connection.
	/// </summary>
	public required string ConnectionName { get; init; }

	/// <summary>
	/// Average events per second on this connection.
	/// </summary>
	public int AverageItemsPerSecond { get; init; }

	/// <summary>
	/// Total items on this connection.
	/// </summary>
	public long TotalItems { get; init; }

	/// <summary>
	/// Number of items seen since last measurement on this connection (used as the basis for averageItemsPerSecond).
	/// </summary>
	public long CountSinceLastMeasurement { get; init; }

	/// <summary>
	/// Number of available slots.
	/// </summary>
	public int AvailableSlots { get; init; }

	/// <summary>
	/// Number of in flight messages on this connection.
	/// </summary>
	public int InFlightMessages { get; init; }

	/// <summary>
	/// Timing measurements for the connection. Can be enabled with the ExtraStatistics setting.
	/// </summary>
	public required IDictionary<string, long> ExtraStatistics { get; init; }

	internal static IEnumerable<PersistentSubscriptionConnectionInfo> CreateFrom(IEnumerable<PersistentSubscriptionConnectionInfoDto> connections) =>
		connections.Select(CreateFrom);

	static PersistentSubscriptionConnectionInfo CreateFrom(PersistentSubscriptionConnectionInfoDto connection) {
		return new PersistentSubscriptionConnectionInfo {
			From                      = connection.From,
			Username                  = connection.Username,
			ConnectionName            = connection.ConnectionName,
			AverageItemsPerSecond     = (int)connection.AverageItemsPerSecond,
			TotalItems                = connection.TotalItems,
			CountSinceLastMeasurement = connection.CountSinceLastMeasurement,
			AvailableSlots            = connection.AvailableSlots,
			InFlightMessages          = connection.InFlightMessages,
			ExtraStatistics           = CreateFrom(connection.ExtraStatistics)
		};
	}

	static Dictionary<string, long> CreateFrom(IEnumerable<PersistentSubscriptionMeasurementInfoDto> extraStatistics) =>
		extraStatistics.ToDictionary(k => k.Key, v => v.Value);
}
