using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Kurrent.Client.Model;
using KurrentDB.Protocol;

namespace Kurrent.Client.Streams;

[PublicAPI]
static class DynamicValueMapper {
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

    public static Metadata MapToMetadata(this DynamicValueMap source) =>
        MapToMetadata(source.Values);

    public static Dictionary<string, string?> MapToDictionary(this MapField<string, DynamicValue> source) {
        return source.Aggregate(
            new Dictionary<string, string?>(),
            (seed, entry) => {
                seed.Add(entry.Key, MapToValueString(entry.Value));
                return seed;
            });

        static string? MapToValueString(DynamicValue source) {
            return source.KindCase switch {
                DynamicValue.KindOneofCase.NullValue      => null,
                DynamicValue.KindOneofCase.None           => null,
                DynamicValue.KindOneofCase.StringValue    => source.StringValue,
                DynamicValue.KindOneofCase.BooleanValue   => source.BooleanValue.ToString(),
                DynamicValue.KindOneofCase.Int32Value     => source.Int32Value.ToString(),
                DynamicValue.KindOneofCase.Int64Value     => source.Int64Value.ToString(),
                DynamicValue.KindOneofCase.FloatValue     => source.FloatValue.ToString(CultureInfo.InvariantCulture),
                DynamicValue.KindOneofCase.DoubleValue    => source.DoubleValue.ToString(CultureInfo.InvariantCulture),
                DynamicValue.KindOneofCase.TimestampValue => source.TimestampValue.ToDateTimeOffset().ToString("O"), // always datetime offset?
                DynamicValue.KindOneofCase.DurationValue  => source.DurationValue.ToTimeSpan().ToString("c"),
                DynamicValue.KindOneofCase.BytesValue     => source.BytesValue.ToBase64(),
                _                                         => throw new NotSupportedException($"Unsupported value type: {source.KindCase}")
            };
        }
    }

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

	public static bool TryMapValue<T>(this DynamicValue source, out T? value) {
		try {
			object? mappedValue = source.KindCase switch {
				DynamicValue.KindOneofCase.NullValue      => null,
				DynamicValue.KindOneofCase.None           => null,
				DynamicValue.KindOneofCase.StringValue    => source.StringValue,
				DynamicValue.KindOneofCase.BooleanValue   => source.BooleanValue,
				DynamicValue.KindOneofCase.Int32Value     => source.Int32Value,
				DynamicValue.KindOneofCase.Int64Value     => source.Int64Value,
				DynamicValue.KindOneofCase.FloatValue     => source.FloatValue,
				DynamicValue.KindOneofCase.DoubleValue    => source.DoubleValue,
				DynamicValue.KindOneofCase.TimestampValue => source.TimestampValue.ToDateTimeOffset(),
				DynamicValue.KindOneofCase.DurationValue  => source.DurationValue.ToTimeSpan(),
				DynamicValue.KindOneofCase.BytesValue     => (ReadOnlyMemory<byte>)source.BytesValue.ToByteArray(),
				_                                         => throw new NotSupportedException($"Unsupported value type: {source.KindCase}")
			};

			if (mappedValue is T typedValue) {
				value = typedValue;
				return true;
			}

			// Handle null case for nullable reference types
			if (mappedValue is null && !typeof(T).IsValueType) {
				value = default(T);
				return true;
			}

			// Handle null case for nullable value types
			if (mappedValue is null && typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>)) {
				value = default(T);
				return true;
			}

			value = default(T);
			return false;
		}
		catch {
			value = default(T);
			return false;
		}
	}

	public static bool TryGetValue<T>(this MapField<string, DynamicValue> source, string key, out T? value) {
		value = default;
		return source.TryGetValue(key, out var dynamicValue) && dynamicValue.TryMapValue(out value);
	}

	public static bool TryGetValue<T>(this DynamicValueMap source, string key, out T? value) =>
		source.Values.TryGetValue<T>(key, out value);

	public static T? GetRequiredValue<T>(this MapField<string, DynamicValue> source, string key) =>
	 		source.TryGetValue(key, out var dynamicValue)
				? dynamicValue.TryMapValue<T>(out var value)
					? value
					: throw new InvalidCastException($"Cannot cast `{key}` to {typeof(T).Name}")
			: throw new KeyNotFoundException($"Required value '{key}' is missing in the source map.");

	public static T? GetRequiredValue<T>(this DynamicValueMap source, string key) =>
		source.Values.GetRequiredValue<T>(key);
}
