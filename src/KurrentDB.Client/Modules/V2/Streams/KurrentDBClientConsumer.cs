using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;

namespace KurrentDB.Client;

#pragma warning disable CS8509
public static class KurrentDBClientConsumer {
	static LegacyProtocolMapper LegacyMapper { get; set; } = null!;
	static ISchemaSerializer        SchemaSerializer { get; set; } = null!;
	static IMetadataDecoder         MetadataDecoder  { get; set; } = null!;

	public static async ValueTask<ResolvedEvent> Consume(
		this KurrentDBClient client,
		Position startPosition,
		ConsumeFilter filter,
		CancellationToken cancellationToken = default
	) {
		//var filterOptions = filter.IsEmptyFilter ? null : new SubscriptionFilterOptions(filter.ToEventFilter());

		await using var result = client.SubscribeToAll(
			FromAll.After(startPosition),
			filterOptions: filter.ToFilterOptions(),
			cancellationToken: cancellationToken
		);

		await foreach (var msg in result.Messages.WithCancellation(cancellationToken)) {
			if (cancellationToken.IsCancellationRequested)
				break;

			if (msg is StreamMessage.LastAllStreamPosition)
				break;

			if (msg is not StreamMessage.Event evt)
				continue;

			return evt.ResolvedEvent;
		}

		return default;
	}
}
