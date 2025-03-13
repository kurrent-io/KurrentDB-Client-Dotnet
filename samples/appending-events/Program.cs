using KurrentDB.Client;

#pragma warning disable CS8321 // Local function is declared but never used

var settings = KurrentDBClientSettings.Create("esdb://localhost:2113?tls=false");

settings.OperationOptions.ThrowOnAppendFailure = false;

await using var client = new KurrentDBClient(settings);

await AppendToStream(client);
await AppendWithConcurrencyCheck(client);
await AppendWithNoStream(client);
await AppendWithSameId(client);

return;

static async Task AppendToStream(KurrentDBClient client) {
	#region append-to-stream

	var eventData = MessageData.From(
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"some-stream",
		StreamState.NoStream,
		[eventData]
	);

	#endregion append-to-stream
}

static async Task AppendWithSameId(KurrentDBClient client) {
	#region append-duplicate-event

	var eventData = MessageData.From(
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		[eventData]
	);

	// attempt to append the same event again
	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		[eventData]
	);

	#endregion append-duplicate-event
}

static async Task AppendWithNoStream(KurrentDBClient client) {
	#region append-with-no-stream

	var eventDataOne = MessageData.From(
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	var eventDataTwo = MessageData.From(
		"some-event",
		"{\"id\": \"2\" \"value\": \"some other value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		StreamState.NoStream,
		[eventDataOne]
	);

	// attempt to append the same event again
	await client.AppendToStreamAsync(
		"no-stream-stream",
		StreamState.NoStream,
		[eventDataTwo]
	);

	#endregion append-with-no-stream
}

static async Task AppendWithConcurrencyCheck(KurrentDBClient client) {
	await client.AppendToStreamAsync(
		"concurrency-stream",
		StreamState.Any,
		[MessageData.From("-", ReadOnlyMemory<byte>.Empty)]
	);

	#region append-with-concurrency-check

	var clientOneRead = client.ReadStreamAsync(
		Direction.Forwards,
		"concurrency-stream",
		StreamPosition.Start
	);

	var clientOneRevision = (await clientOneRead.LastAsync()).Event.EventNumber.ToUInt64();

	var clientTwoRead     = client.ReadStreamAsync(Direction.Forwards, "concurrency-stream", StreamPosition.Start);
	var clientTwoRevision = (await clientTwoRead.LastAsync()).Event.EventNumber.ToUInt64();

	var clientOneData = MessageData.From(
		"some-event",
		"{\"id\": \"1\" \"value\": \"clientOne\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		clientOneRevision,
		[clientOneData]
	);

	var clientTwoData = MessageData.From(
		"some-event",
		"{\"id\": \"2\" \"value\": \"clientTwo\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		"no-stream-stream",
		clientTwoRevision,
		[clientTwoData]
	);

	#endregion append-with-concurrency-check
}

static async Task AppendOverridingUserCredentials(KurrentDBClient client, CancellationToken cancellationToken) {
	var eventData = MessageData.From(
		"TestEvent",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	#region overriding-user-credentials

	await client.AppendToStreamAsync(
		"some-stream",
		StreamState.Any,
		[eventData],
		new AppendToStreamOptions { UserCredentials = new UserCredentials("admin", "changeit") },
		cancellationToken
	);

	#endregion overriding-user-credentials
}
