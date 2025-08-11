using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;
using KurrentDB.Protocol;

namespace KurrentDB.Client;

[PublicAPI]
static class DynamicValueMapper {
	public static MapField<string, DynamicValue> MapToDynamicMapField(this Dictionary<string, object?> source) =>
		source.Aggregate(
			new MapField<string, DynamicValue>(),
			(seed, entry) => {
				seed.Add(entry.Key, MapToDynamicValue(entry.Value));
				return seed;
			}
		);

	public static DynamicValue MapToDynamicValue(this object? source) {
		return source switch {
			null     => new() { NullValue    = NullValue.NullValue },
			string x => new() { StringValue  = x },
			bool x   => new() { BooleanValue = x },
			int x    => new() { Int32Value   = x },
			long x   => new() { Int64Value   = x },
			float x  => new() { FloatValue   = x },
			double x => new() { DoubleValue  = x },

			DateTime x       => new() { TimestampValue = Timestamp.FromDateTime(x.Kind is DateTimeKind.Utc ? x : DateTime.SpecifyKind(x, DateTimeKind.Utc)) },
			DateTimeOffset x => new() { TimestampValue = Timestamp.FromDateTimeOffset(x) },
			TimeSpan x       => new() { DurationValue  = x.ToDuration() },

			byte[] x               => new() { BytesValue = ByteString.CopyFrom(x) },
			ReadOnlyMemory<byte> x => new() { BytesValue = ByteString.CopyFrom(x.Span) },

			_ => new() { StringValue = source.ToString() } // any other type is converted to string
		};
	}
}
