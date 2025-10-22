using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
static class ValueMapper {
	public static MapField<string, Value> MapToMapValue(this Dictionary<string, string> source) =>
		source.Aggregate(
			new MapField<string, Value>(),
			(seed, entry) => {
				seed.Add(entry.Key, Value.ForString(entry.Value));
				return seed;
			}
		);
}
