﻿#pragma warning disable CS8321 // Local function is declared but never used

using EventTypeFilter = KurrentDB.Client.EventTypeFilter;

const int eventCount = 100;

var semaphore = new SemaphoreSlim(eventCount);

await using var client = new KurrentDBClient(KurrentDBClientSettings.Create("kurrentdb://localhost:2113?tls=false"));

_ = Task.Run(async () => {
	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		filterOptions: new SubscriptionFilterOptions(EventTypeFilter.Prefix("some-")));
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				semaphore.Release();
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
});


await Task.Delay(2000);

for (var i = 0; i < eventCount; i++) {
	var eventData = new EventData(
		Uuid.NewUuid(),
		i % 2 == 0 ? "some-event" : "other-event",
		"{\"id\": \"1\" \"value\": \"some value\"}"u8.ToArray()
	);

	await client.AppendToStreamAsync(
		Guid.NewGuid().ToString("N"),
		StreamState.Any,
		new List<EventData> { eventData }
	);
}

await semaphore.WaitAsync();

return;

static async Task ExcludeSystemEvents(KurrentDBClient client) {
	#region exclude-system

	await using var subscription = client.SubscribeToAll(
		FromAll.Start,
		filterOptions: new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()));
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}

	#endregion exclude-system
}

static async Task EventTypePrefix(KurrentDBClient client) {
	#region event-type-prefix

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.Prefix("customer-"));

	#endregion event-type-prefix

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task EventTypeRegex(KurrentDBClient client) {
	#region event-type-regex

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.RegularExpression("^user|^company"));

	#endregion event-type-regex

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task StreamPrefix(KurrentDBClient client) {
	#region stream-prefix

	var filterOptions = new SubscriptionFilterOptions(StreamFilter.Prefix("user-"));

	#endregion stream-prefix

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task StreamRegex(KurrentDBClient client) {
	#region stream-regex

	var filterOptions = new SubscriptionFilterOptions(StreamFilter.RegularExpression("^account|^savings"));

	#endregion stream-regex

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
		}
	}
}

static async Task CheckpointCallback(KurrentDBClient client) {
	#region checkpoint

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents());

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
	
	#endregion checkpoint
}

static async Task CheckpointCallbackWithInterval(KurrentDBClient client) {
	#region checkpoint-with-interval

	var filterOptions = new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents(), 1000);

	#endregion checkpoint-with-interval

	await using var subscription = client.SubscribeToAll(FromAll.Start, filterOptions: filterOptions);
	await foreach (var message in subscription.Messages) {
		switch (message) {
			case StreamMessage.Event(var e):
				Console.WriteLine($"{e.Event.EventType} @ {e.Event.Position.PreparePosition}");
				break;
			case StreamMessage.AllStreamCheckpointReached(var p):
				Console.WriteLine($"checkpoint taken at {p.PreparePosition}");
				break;
		}
	}
}
