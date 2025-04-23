var tokenSource       = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

#region createClient

const string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";

var settings = KurrentDBClientSettings.Create(connectionString);

var client = new KurrentDBClient(settings);

#endregion createClient

#region createEvent

var evt = new TestEvent {
	EntityId      = Guid.NewGuid().ToString("N"),
	ImportantData = "I wrote my first event!"
};

#endregion createEvent

#region appendEvents

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	[evt],
	cancellationToken: cancellationToken
);

#endregion appendEvents

#region overriding-user-credentials

await client.AppendToStreamAsync(
	"some-stream",
	StreamState.Any,
	[evt],
	new AppendToStreamOptions { UserCredentials = new UserCredentials("admin", "changeit") },
	cancellationToken
);

#endregion overriding-user-credentials

#region readStream

var result = client.ReadStreamAsync(
	"some-stream",
	cancellationToken: cancellationToken
);

var events = await result
	.DeserializedData()
	.ToListAsync(cancellationToken);

#endregion readStream

public class TestEvent {
	public string? EntityId      { get; set; }
	public string? ImportantData { get; set; }
}
