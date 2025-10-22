using EventStore.Client;
using Google.Protobuf.WellKnownTypes;
using KurrentDB.Client;

namespace KurrentDB.Protocol.Streams.V1 {
	partial class BatchAppendReq {
		partial class Types {
			partial class Options {
				public static Options Create(StreamIdentifier streamIdentifier, StreamState expectedState,
					TimeSpan? timeoutAfter) {
					if (expectedState.HasPosition) {
						return new() {
							StreamIdentifier = streamIdentifier,
							StreamPosition   = (ulong)expectedState.ToInt64(),
							Deadline21100 = Timestamp.FromDateTime(
								timeoutAfter.HasValue
									? DateTime.UtcNow + timeoutAfter.Value
									: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
							)
						};
					}

					return new Options {
						StreamIdentifier = streamIdentifier,
						expectedStreamPositionCase_ = expectedState switch {
							{ } when expectedState == StreamState.Any      => ExpectedStreamPositionOneofCase.Any,
							{ } when expectedState == StreamState.NoStream => ExpectedStreamPositionOneofCase.NoStream,
							{ } when expectedState == StreamState.StreamExists => ExpectedStreamPositionOneofCase
								.StreamExists,
							_ => ExpectedStreamPositionOneofCase.StreamPosition
						},
						expectedStreamPosition_ = new Google.Protobuf.WellKnownTypes.Empty(),
						Deadline21100 = Timestamp.FromDateTime(
							timeoutAfter.HasValue
								? DateTime.UtcNow + timeoutAfter.Value
								: DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
						)
					};
				}
			}
		}
	}
}
