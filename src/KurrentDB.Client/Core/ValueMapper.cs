using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
static class ValueMapper {
	public static MapField<string, Value> MapToMapValue(this Dictionary<string, object?> source) =>
		source.Aggregate(
			new MapField<string, Value>(),
			(seed, entry) => {
				seed.Add(entry.Key, MapToValue(entry.Value));
				return seed;
			}
		);

	public static Value MapToValue(this object? source) {
		return source switch {
			null     => new() { NullValue   = NullValue.NullValue },
			string x => new() { StringValue = x },
			bool x   => new() { BoolValue   = x },
			int x    => new() { NumberValue = x },
			long x   => new() { NumberValue = x },
			float x  => new() { NumberValue = x },
			double x => new() { NumberValue = x },

			DateTime x       => new() { StringValue = x.ToUniversalTime().ToString("O") },
			DateTimeOffset x => new() { StringValue = x.ToUniversalTime().ToString("O") },
			TimeSpan x       => new() { StringValue = x.ToString() },

			byte[] x => new() { StringValue = Convert.ToBase64String(x) },
			ReadOnlyMemory<byte> x => new() {
#if NET48
				StringValue = Convert.ToBase64String(x.ToArray())
#else
				StringValue = Convert.ToBase64String(x.Span)
#endif
			},

			_ => new() { StringValue = source.ToString() } // any other type is converted to string
		};
	}
}
