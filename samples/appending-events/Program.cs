#pragma warning disable CS8321 // Local function is declared but never used

var settings = KurrentDBClientSettings.Create("esdb://localhost:2113?tls=false");

settings.OperationOptions.ThrowOnAppendFailure = false;

await using var client = new KurrentDBClient(settings);

await AppendToStream(client);
await AppendToStreamWithAutoSerialization(client);
await AppendToStreamWithMetadataAndAutoSerialization(client);
await AppendWithConcurrencyCheck(client);
await AppendWithNoStream(client);
await AppendWithSameId(client);
await AppendWithSameIdAndAutoSerialization(client);

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

static async Task AppendToStreamWithAutoSerialization(KurrentDBClient client) {
	#region append-to-stream-with-auto-serialization

	var shoppingCartId = Guid.NewGuid();

	var @event = new ProductItemAddedToShoppingCart(
		shoppingCartId,
		new PricedProductItem("t-shirt", 1, 99.99m)
	);

	await client.AppendToStreamAsync(
		$"shopping_cart-{shoppingCartId}",
		StreamState.NoStream,
		[@event]
	);

	#endregion append-to-stream-with-auto-serialization
}

static async Task AppendToStreamWithMetadataAndAutoSerialization(KurrentDBClient client) {
	#region append-to-stream-with-metadata-and-auto-serialization

	var shoppingCartId = Guid.NewGuid();
	var clientId       = Guid.NewGuid().ToString();

	var @event = new ProductItemAddedToShoppingCart(
		shoppingCartId,
		new PricedProductItem("t-shirt", 1, 99.99m)
	);

	var metadata = new ShoppingCartMetadata(clientId);

	var message = Message.From(@event, metadata);

	await client.AppendToStreamAsync(
		$"shopping_cart-{shoppingCartId}",
		StreamState.NoStream,
		[message]
	);

	#endregion append-to-stream-with-metadata-and-auto-serialization
}

static async Task AppendWithSameId(KurrentDBClient client) {
	#region append-duplicate-event

	var eventData = MessageData.From(
		"some-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray(),
		messageId: Uuid.NewUuid()
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

static async Task AppendWithSameIdAndAutoSerialization(KurrentDBClient client) {
	#region append-duplicate-event-with-serialization

	var shoppingCartId = Guid.NewGuid();

	var @event = new ProductItemAddedToShoppingCart(
		shoppingCartId,
		new PricedProductItem("t-shirt", 1, 99.99m)
	);

	var message = Message.From(
		@event,
		messageId: Uuid.NewUuid()
	);

	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		[message]
	);

	// attempt to append the same event again
	await client.AppendToStreamAsync(
		"same-event-stream",
		StreamState.Any,
		[message]
	);

	#endregion append-duplicate-event-with-serialization
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

	var clientOneRead = client.ReadStreamAsync("concurrency-stream");

	var clientOneRevision = (await clientOneRead.LastAsync()).Event.EventNumber.ToUInt64();

	var clientTwoRead     = client.ReadStreamAsync("concurrency-stream");
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

public record PricedProductItem(
	string ProductId,
	int Quantity,
	decimal UnitPrice
);

public record ProductItemAddedToShoppingCart(
	Guid CartId,
	PricedProductItem ProductItem
);

public record ShoppingCartMetadata(string ClientId);
