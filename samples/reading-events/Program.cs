using KurrentDB.Client;

#pragma warning disable CS8321 // Local function is declared but never used

await using var client = new KurrentDBClient(KurrentDBClientSettings.Create("esdb://localhost:2113?tls=false"));

var events = Enumerable.Range(0, 20)
	.Select(
		r => MessageData.From(
			"some-event",
			Encoding.UTF8.GetBytes($"{{\"id\": \"{r}\" \"value\": \"some value\"}}")
		)
	);

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	events
);

await ReadFromStream(client);

return;

static async Task ReadFromStream(KurrentDBClient client) {
	#region read-from-stream

	var events = client.ReadStreamAsync("some-stream");

	#endregion read-from-stream

	#region iterate-stream

	await foreach (var @event in events) Console.WriteLine(@event.DeserializedData);

	#endregion iterate-stream

	#region #read-from-stream-positions

	Console.WriteLine(events.FirstStreamPosition);
	Console.WriteLine(events.LastStreamPosition);

	#endregion
}

static async Task ReadFromStreamMessages(KurrentDBClient client) {
	#region read-from-stream-messages

	var results = client.ReadStreamAsync("some-stream");

	#endregion read-from-stream-messages

	#region iterate-stream-messages

	var streamPosition = StreamPosition.Start;

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Ok ok:
				Console.WriteLine("Stream found.");
				break;

			case StreamMessage.NotFound:
				Console.WriteLine("Stream not found.");
				return;

			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.FirstStreamPosition(var sp):
				Console.WriteLine($"{sp} is after {streamPosition}; updating checkpoint.");
				streamPosition = sp;
				break;

			case StreamMessage.LastStreamPosition(var sp):
				Console.WriteLine($"The end of the stream is {sp}");
				break;

			default:
				break;
		}

	#endregion iterate-stream-messages
}

static async Task ReadFromStreamPosition(KurrentDBClient client) {
	#region read-from-stream-position

	var events = client.ReadStreamAsync(
		"some-stream",
		new ReadStreamOptions { StreamPosition = 10, MaxCount = 20 }
	);

	#endregion read-from-stream-position

	#region iterate-stream

	await foreach (var @event in events) Console.WriteLine(@event.DeserializedData);

	#endregion iterate-stream
}

static async Task ReadFromStreamPositionCheck(KurrentDBClient client) {
	#region checking-for-stream-presence

	var result = client.ReadStreamAsync(
		"some-stream",
		new ReadStreamOptions { StreamPosition = 10, MaxCount = 20 }
	);

	if (await result.ReadState == ReadState.StreamNotFound) return;

	await foreach (var e in result) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion checking-for-stream-presence
}

static async Task ReadFromStreamBackwards(KurrentDBClient client) {
	#region reading-backwards

	var events = client.ReadStreamAsync(
		"some-stream",
		new ReadStreamOptions { Direction = Direction.Backwards, StreamPosition = StreamPosition.End }
	);

	await foreach (var e in events) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion reading-backwards
}

static async Task ReadFromStreamMessagesBackwards(KurrentDBClient client) {
	#region read-from-stream-messages-backwards

	var results = client.ReadStreamAsync(
		"some-stream",
		new ReadStreamOptions { Direction = Direction.Backwards, StreamPosition = StreamPosition.End }
	);

	#endregion read-from-stream-messages-backwards

	#region iterate-stream-messages-backwards

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Ok ok:
				Console.WriteLine("Stream found.");
				break;

			case StreamMessage.NotFound:
				Console.WriteLine("Stream not found.");
				return;

			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastStreamPosition(var sp):
				Console.WriteLine($"The end of the stream is {sp}");
				break;
		}

	#endregion iterate-stream-messages-backwards
}

static async Task ReadFromAllStream(KurrentDBClient client) {
	#region read-from-all-stream

	var events = client.ReadAllAsync();

	#endregion read-from-all-stream

	#region read-from-all-stream-iterate

	await foreach (var e in events) Console.WriteLine(e.DeserializedData);

	#endregion read-from-all-stream-iterate
}

static async Task ReadFromAllStreamMessages(KurrentDBClient client) {
	#region read-from-all-stream-messages

	var position = Position.Start;
	var results  = client.ReadAllAsync(new ReadAllOptions { Position = position });

	#endregion read-from-all-stream-messages

	#region iterate-all-stream-messages

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastAllStreamPosition(var p):
				Console.WriteLine($"The end of the $all stream is {p}");
				break;
		}

	#endregion iterate-all-stream-messages
}

static async Task IgnoreSystemEvents(KurrentDBClient client) {
	#region ignore-system-events

	var events = client.ReadAllAsync();

	await foreach (var e in events) {
		if (e.Event.EventType.StartsWith("$")) continue;

		Console.WriteLine(e.DeserializedData);
	}

	#endregion ignore-system-events
}

static async Task ReadFromAllStreamBackwards(KurrentDBClient client) {
	#region read-from-all-stream-backwards

	var events = client.ReadAllAsync(new ReadAllOptions { Direction = Direction.Backwards, Position = Position.End });

	#endregion read-from-all-stream-backwards

	#region read-from-all-stream-iterate

	await foreach (var e in events) Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));

	#endregion read-from-all-stream-iterate
}

static async Task ReadFromAllStreamBackwardsMessages(KurrentDBClient client) {
	#region read-from-all-stream-messages-backwards

	var position = Position.End;
	var results  = client.ReadAllAsync(new ReadAllOptions { Direction = Direction.Backwards, Position = position });

	#endregion read-from-all-stream-messages-backwards

	#region iterate-all-stream-messages-backwards

	await foreach (var message in results.Messages)
		switch (message) {
			case StreamMessage.Event(var resolvedEvent):
				Console.WriteLine(Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span));
				break;

			case StreamMessage.LastAllStreamPosition(var p):
				Console.WriteLine($"{p} is before {position}; updating checkpoint.");
				position = p;
				break;
		}

	#endregion iterate-all-stream-messages-backwards
}

static async Task FilteringOutSystemEvents(KurrentDBClient client) {
	var events = client.ReadAllAsync();

	await foreach (var e in events)
		if (!e.Event.EventType.StartsWith("$"))
			Console.WriteLine(Encoding.UTF8.GetString(e.Event.Data.ToArray()));
}

static void ReadStreamOverridingUserCredentials(KurrentDBClient client, CancellationToken cancellationToken) {
	#region overriding-user-credentials

	var result = client.ReadStreamAsync(
		"some-stream",
		new ReadStreamOptions { UserCredentials = new UserCredentials("admin", "changeit") },
		cancellationToken: cancellationToken
	);

	#endregion overriding-user-credentials
}

static void ReadAllOverridingUserCredentials(KurrentDBClient client, CancellationToken cancellationToken) {
	#region read-all-overriding-user-credentials

	var result = client.ReadAllAsync(
		new ReadAllOptions { UserCredentials = new UserCredentials("admin", "changeit") },
		cancellationToken: cancellationToken
	);

	#endregion read-all-overriding-user-credentials
}

static void ReadAllResolvingLinkTos(KurrentDBClient client, CancellationToken cancellationToken) {
	#region read-from-all-stream-resolving-link-Tos

	var result = client.ReadAllAsync(
		new ReadAllOptions { ResolveLinkTos = true },
		cancellationToken: cancellationToken
	);

	#endregion read-from-all-stream-resolving-link-Tos
}
