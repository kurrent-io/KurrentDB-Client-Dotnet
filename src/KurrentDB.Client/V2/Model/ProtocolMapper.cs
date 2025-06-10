using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client.Model;

[PublicAPI]
static class ProtocolMapper {
	public static DynamicValueMap MapToDynamicValueMap(this Metadata source) =>
		new() { Values = { source.MapToDynamicMapField() } };

	public static MapField<string, DynamicValue> MapToDynamicMapField(this Metadata source) =>
		source.Aggregate(new MapField<string, DynamicValue>(), (seed, entry) => {
			seed.Add(entry.Key, MapToDynamicValue(entry.Value));
			return seed;
		});

	public static DynamicValue MapToDynamicValue(this object? source) {
		return source switch {
			null     => new() { NullValue    = NullValue.NullValue },
			string x => new() { StringValue  = x },
			bool x   => new() { BooleanValue = x },
			int x    => new() { Int32Value   = x },
			long x   => new() { Int64Value   = x },
			float x  => new() { FloatValue   = x },
			double x => new() { DoubleValue  = x },

			DateTime x       => new() { TimestampValue = x.ToTimestamp() },
			DateTimeOffset x => new() { TimestampValue = x.ToTimestamp() },
			TimeSpan x       => new() { DurationValue  = x.ToDuration() },

			byte[] x               => new() { BytesValue = ByteString.CopyFrom(x) },
			ReadOnlyMemory<byte> x => new() { BytesValue = ByteString.CopyFrom(x.Span) },

			_ => new() { StringValue = source.ToString() } // any other type is converted to string
		};
	}

	public static Metadata MapToMetadata(this MapField<string, DynamicValue> source) =>
		source.Aggregate(new Metadata(), (seed, entry) => {
			seed.With(entry.Key, MapToMetadataValue(entry.Value));
			return seed;
		});

	public static object? MapToMetadataValue(this DynamicValue source) {
		return source.KindCase switch {
			DynamicValue.KindOneofCase.NullValue      => null,
			DynamicValue.KindOneofCase.None           => null,
			DynamicValue.KindOneofCase.StringValue    => source.StringValue,
			DynamicValue.KindOneofCase.BooleanValue   => source.BooleanValue,
			DynamicValue.KindOneofCase.Int32Value     => source.Int32Value,
			DynamicValue.KindOneofCase.Int64Value     => source.Int64Value,
			DynamicValue.KindOneofCase.FloatValue     => source.FloatValue,
			DynamicValue.KindOneofCase.DoubleValue    => source.DoubleValue,
			DynamicValue.KindOneofCase.TimestampValue => source.TimestampValue.ToDateTimeOffset(), // always datetime offset?
			DynamicValue.KindOneofCase.DurationValue  => source.DurationValue.ToTimeSpan(),
			DynamicValue.KindOneofCase.BytesValue     => (ReadOnlyMemory<byte>) source.BytesValue.ToByteArray(),
			_                                         => throw new NotSupportedException($"Unsupported value type: {source.KindCase}")
		};
	}
}
