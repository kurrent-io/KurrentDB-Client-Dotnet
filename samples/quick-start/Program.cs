using System.Text.Json;
using Kurrent;
using Kurrent.Client;
using Kurrent.Client.Model;

var tokenSource       = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

var evt = new TestEvent {
	EntityId      = Guid.NewGuid().ToString("N"),
	ImportantData = "I wrote my first event!"
};

#region createClient

const string connectionString = "esdb://admin:changeit@localhost:2113?tls=false&tlsVerifyCert=false";

// just using the conn strig with defaults
// var client = KurrentClient.Create(connectionString);

var client = KurrentClientOptions.Build
	.WithConnectionString(connectionString)
	.WithMessages(map => map.Map<TestEvent>())
	.WithResilience(KurrentClientResilienceOptions.FailFast)
	.CreateClient();

#endregion createClient

#region createEvent

var message = Message.Create(evt);

var message2 = Message.New
	.WithMetadata("my-shit", 849)
	.Build();

// var eventData = new EventData(
// 	Uuid.NewUuid(),
// 	"TestEvent",
// 	JsonSerializer.SerializeToUtf8Bytes(evt)
// );

#endregion createEvent

#region appendEvents

// var result = await client.Streams
// 	.Append("some-stream", ExpectedStreamState.NoStream, message, cancellationToken);


var success = await client.Streams
	.Append("some-stream", ExpectedStreamState.NoStream, message, cancellationToken)
	.ThrowOnFailureAsync();

// success.Stream, success.Position, success.StreamRevision


// await client.AppendToStreamAsync(
// 	"some-stream",
// 	StreamState.Any,
// 	new[] { eventData },
// 	cancellationToken: cancellationToken
// );

#endregion appendEvents

#region readStream

// var records =  await client.Streams
// 	.ReadStreamAsync(new() {
// 		Stream            = "some-stream",
// 		CancellationToken = cancellationToken })
// 	.ToListAsync();
//
//
// var result =  await client.Streams
// 	.ReadStream(new() {
// 		Stream            = "some-stream",
// 		CancellationToken = cancellationToken });
//
// var messages = result.ThrowOnFailure();
//
// await foreach (var msg in messages) {
// 	// var omg = msg.Value switch {
// 	// 	Record rec   => rec,
// 	// 	Heartbeat he => he,
// 	// };
// 	//
// 	// switch (msg.Case) {
// 	// 	case ReadMessage.ReadMessageCase.Record:
// 	// 		break;
// 	//
// 	// 	case ReadMessage.ReadMessageCase.Heartbeat: break;
// 	//
// 	// 	default: throw new ArgumentOutOfRangeException();
// 	// }
// 	//
// 	//
// 	// msg.Switch(record => { }, heartbeat => { });
// 	//
// 	// await msg.SwitchAsync(record => { }, heartbeat => { });
// 	//
// 	// msg.Match(record => { }, heartbeat => { });
// 	//
// 	// msg.MatchAsync(record => { }, heartbeat => { });
//
// }



// var resultOld = client.ReadStreamAsync(
// 	Direction.Forwards,
// 	"some-stream",
// 	StreamPosition.Start,
// 	cancellationToken: cancellationToken
// );
//
// var events = await resultOld.ToListAsync(cancellationToken);

#endregion readStream


public class TestEvent {
	public string? EntityId      { get; set; }
	public string? ImportantData { get; set; }
}
