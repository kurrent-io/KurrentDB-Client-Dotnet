﻿using System.Text.Json;

var tokenSource       = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

#region createClient

const string connectionString = "kurrentdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";

var settings = KurrentDBClientSettings.Create(connectionString);

var client = new KurrentDBClient(settings);

#endregion createClient

#region createEvent

var evt = new TestEvent {
	EntityId      = Guid.NewGuid().ToString("N"),
	ImportantData = "I wrote my first event!"
};

var eventData = new EventData(
	Uuid.NewUuid(),
	"TestEvent",
	JsonSerializer.SerializeToUtf8Bytes(evt)
);

#endregion createEvent

#region appendEvents

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	new[] { eventData },
	cancellationToken: cancellationToken
);

#endregion appendEvents

#region overriding-user-credentials

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	new[] { eventData },
	userCredentials: new UserCredentials("admin", "changeit"),
	cancellationToken: cancellationToken
);

#endregion overriding-user-credentials

#region readStream

var result = client.ReadStreamAsync(
	Direction.Forwards,
	"some-stream",
	StreamPosition.Start,
	cancellationToken: cancellationToken
);

var events = await result.ToListAsync(cancellationToken);

#endregion readStream

public class TestEvent {
	public string? EntityId      { get; set; }
	public string? ImportantData { get; set; }
}
