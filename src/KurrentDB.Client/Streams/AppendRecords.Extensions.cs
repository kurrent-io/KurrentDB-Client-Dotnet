namespace KurrentDB.Client;

public static class AppendRecordsExtensions {
	/// <summary>
	/// Appends records to one or more streams atomically with cross-stream consistency checks.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		IEnumerable<AppendRecord> records,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync(records, checks: null, cancellationToken);

	/// <summary>
	/// Appends a single record to a stream atomically.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		AppendRecord record,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync([record], checks: null, cancellationToken);

	/// <summary>
	/// Appends records to a single stream atomically.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		string stream,
		IEnumerable<EventData> events,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync(
			events.Select(e => new AppendRecord(stream, e)),
			checks: null,
			cancellationToken
		);

	/// <summary>
	/// Appends records to a single stream atomically with a consistency check.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		string stream,
		StreamState expectedState,
		IEnumerable<EventData> events,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync(
			events.Select(e => new AppendRecord(stream, e)),
			[new ConsistencyCheck.StreamStateCheck(stream, expectedState)],
			cancellationToken
		);

	/// <summary>
	/// Appends a single record to a stream atomically with a consistency check.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		AppendRecord record,
		ConsistencyCheck check,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync([record], [check], cancellationToken);

	/// <summary>
	/// Appends records to one or more streams atomically with consistency checks.
	/// </summary>
	public static ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		this KurrentDBClient client,
		IEnumerable<AppendRecord> records,
		IEnumerable<ConsistencyCheck> checks,
		CancellationToken cancellationToken = default
	) =>
		client.AppendRecordsAsync(records, checks, cancellationToken);
}
