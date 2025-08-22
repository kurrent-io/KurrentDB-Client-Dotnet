namespace KurrentDB.Client;

public static class MultiStreamAppendExtensions {
	public static ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		this KurrentDBClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default
	) =>
		client.MultiStreamAppendAsync(requests.ToAsyncEnumerable(), cancellationToken);

	public static ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		this KurrentDBClient client, AppendStreamRequest request, CancellationToken cancellationToken = default
	) =>
		client.MultiStreamAppendAsync([request], cancellationToken);

	/// <summary>
	/// Appends a series of messages to a specified stream while specifying the expected stream state.
	/// </summary>
	/// <param name="stream">The name of the stream to which the messages will be appended.</param>
	/// <param name="expectedState">The expected state of the stream to ensure consistency during the append operation.</param>
	/// <param name="messages">A collection of messages to be appended to the stream.</param>
	/// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation if needed.</param>
	public static ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		this KurrentDBClient client, string stream, StreamState expectedState, IEnumerable<EventData> messages,
		CancellationToken cancellationToken
	) =>
		client.MultiStreamAppendAsync(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

	public static ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		this KurrentDBClient client, string stream, StreamState expectedState, EventData message,
		CancellationToken cancellationToken
	) =>
		client.MultiStreamAppendAsync(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

	public static ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		this KurrentDBClient client, string stream, IEnumerable<EventData> messages, CancellationToken cancellationToken
	) =>
		client.MultiStreamAppendAsync(new AppendStreamRequest(stream, StreamState.Any, messages), cancellationToken);
}
