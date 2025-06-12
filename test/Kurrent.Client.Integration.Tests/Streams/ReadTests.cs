// using Kurrent.Client.Legacy;
// using Kurrent.Client.Model;
// using Kurrent.Client.SchemaRegistry;
// using Kurrent.Client.SchemaRegistry.Serialization;
// using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
// using Kurrent.Client.SchemaRegistry.Serialization.Json;
// using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
// using TicTacToe;
//
// namespace Kurrent.Client.Tests;
//
// public class ReadTests : KurrentClientTestFixture {
//     [Test]
//     public async Task reads_stream(CancellationToken ct) {
//         // Arrange
//         var stream = $"Game-{Guid.NewGuid().ToString().Substring(24, 12)}";
//
//         var msg = new GameStarted(Guid.NewGuid(), Player.X);
//
//         // create a stream with at least 10 messages to ensure we can read it
//         // but I must use the Legacy client for this, because the new client requires multi stream appends
//
//         var serializerProvider = new SchemaSerializerProvider(
//             [
//                 new BytesPassthroughSerializer(),
//                 new JsonSchemaSerializer(
//                     new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
//                     schemaManager
//                 ),
//                 new ProtobufSchemaSerializer(
//                     new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
//                     schemaManager
//                 )
//             ]
//         );
//
//         var messages = Enumerable
//             .Range(0, 10)
//             .Select(i => {
//                 var evt = new GameStarted(Guids.CreateVersion7(), i % 2 == 0 ? Player.X : Player.O);
//                 return Message.New().WithValue(evt).Build();
//             })
//             .ConvertAllToEventData(stream, new SchemaSerializerProvider());
//
//         var eventData = messages
//             .Select(m => )
//             .ToList();
//
//         var appendTask = LegacyClient
//             .AppendToStreamAsync(stream, StreamRevision.NoStream, msg, ct)
//             .AsTask();
//
//
//
//         // var thing = new List<AppendStreamRequest>(
//         //     [
//         //         new AppendStreamRequest(
//         //             stream, ExpectedStreamState.NoStream, [
//         //                 new Message {
//         //                     Value = msg
//         //                 }
//         //             ]
//         //         )
//         //     ]
//         // ).ToAsyncEnumerable();
//                    // Act
//                    var appendTask = AutomaticClient.Streams
//             .Append(stream, StreamRevision.From(1), msg, ct)
//             .AsTask();
//
//         await Should.NotThrowAsync(() => appendTask);
//
//         var result = await appendTask;
//
//         // Assert
//         result
//             .OnSuccess(success => {
//                 success.Stream.ShouldBe(stream);
//                 success.StreamRevision.ShouldBeGreaterThanOrEqualTo(StreamRevision.Min);
//                 success.Position.ShouldBeGreaterThanOrEqualTo(LogPosition.Earliest);
//             })
//             .OnError(failure => Assert.Fail(failure.Value.ToString()!));
//     }
// }
