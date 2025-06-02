using System.Runtime.CompilerServices;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using KurrentDB.Client;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Kurrent.Client.Model;

static class LegacyProtocolMapping {
	public static async ValueTask<EventData> ConvertToEventData(
		this Message message,
		string stream,
		ISchemaSerializerProvider serializerProvider,
		SchemaRegistryPolicy registryPolicy,
		CancellationToken ct
	) {
		var context = new SchemaSerializationContext {
			Stream               = stream,
			Metadata             = message.Metadata,
			SchemaRegistryPolicy = registryPolicy,
			CancellationToken    = ct
		};

		var data = await serializerProvider
			.GetSerializer(message.DataFormat)
			.Serialize(message, context)
			.ConfigureAwait(false);

		// self contained because the new protocol uses a DynamicMapField
		var metadata = JsonSerializer
			.SerializeToUtf8Bytes(message.Metadata, JsonSchemaSerializerOptions.DefaultJsonSerializerOptions);

		var id          = Uuid.FromGuid(message.RecordId);
		var schemaName  = message.Metadata.Get<string>(SystemMetadataKeys.SchemaName);
		var contentType = message.DataFormat.GetContentType();

		// it would be easier to always add it and let the server handle it.
		// the new protocol will require this for automatic schema serde.
		// context.Metadata.Remove(SystemMetadataKeys.SchemaName);

		return new(
			eventId: id,
			type: schemaName,
			data: data,
			metadata: metadata,
			contentType: contentType
		);
	}

	public static async ValueTask<Record> ConvertToRecord(
		this ResolvedEvent resolvedEvent,
		ISchemaSerializerProvider serializerProvider,
		IMetadataDecoder metadataDecoder,
		SchemaRegistryPolicy registryPolicy,
		CancellationToken ct
	) {
		var metadata = metadataDecoder.Decode(resolvedEvent.OriginalEvent.Metadata);

		// Handle backwards compatibility with old data by injecting the legacy schema in the metadata.
		if (!metadata.TryGet<SchemaDataFormat>(SystemMetadataKeys.SchemaDataFormat, out var dataFormat)) {
			metadata.With(SystemMetadataKeys.SchemaName, resolvedEvent.OriginalEvent.EventType);
			metadata.With(SystemMetadataKeys.SchemaDataFormat, dataFormat = resolvedEvent.OriginalEvent.ContentType.GetSchemaDataFormat());
		}

		var context = new SchemaSerializationContext {
			Stream               = resolvedEvent.OriginalEvent.EventStreamId,
			Metadata             = metadata,
			SchemaRegistryPolicy = registryPolicy,
			CancellationToken    = ct
		};

		var value = await serializerProvider
			.GetSerializer(dataFormat)
			.Deserialize(resolvedEvent.OriginalEvent.Data, context)
			.ConfigureAwait(false);

		var record = new Record {
			Id             = resolvedEvent.OriginalEvent.EventId.ToGuid(),
			Position       = resolvedEvent.OriginalPosition.ConvertToLogPosition(),
			Stream         = resolvedEvent.OriginalEvent.EventStreamId,
			StreamRevision = resolvedEvent.OriginalEvent.EventNumber.ToInt64(),
			Timestamp      = resolvedEvent.OriginalEvent.Created,
			Metadata       = metadata,
			Value          = value!,
			ValueType      = value is not null ? value.GetType() : Type.Missing.GetType(),
			Data           = resolvedEvent.OriginalEvent.Data
		};

		return record;
	}

	public static async IAsyncEnumerable<EventData> ConvertAllToEventData(
		this IEnumerable<Message> messages,
		string stream,
		ISchemaSerializerProvider serializerProvider,
		SchemaRegistryPolicy registryPolicy,
		[EnumeratorCancellation] CancellationToken ct
	) {
		foreach (var message in messages)
			yield return await ConvertToEventData(message, stream, serializerProvider, registryPolicy, ct);
	}

	public static async IAsyncEnumerable<Record> ConvertAllToRecord(
		this IEnumerable<ResolvedEvent> resolvedEvents,
		ISchemaSerializerProvider serializerProvider,
		IMetadataDecoder metadataDecoder,
		SchemaRegistryPolicy registryPolicy,
		[EnumeratorCancellation] CancellationToken ct
	) {
		foreach (var re in resolvedEvents)
			yield return await ConvertToRecord(re, serializerProvider, metadataDecoder, registryPolicy, ct);
	}

	/// <summary>
	/// Converts a <see cref="StreamRevision"/> instance to its corresponding <see cref="StreamPosition"/>.
	/// </summary>
	/// <param name="revision">The <see cref="StreamRevision"/> to be converted.</param>
	/// <returns>The corresponding <see cref="StreamPosition"/> based on the provided <see cref="StreamRevision"/> value.</returns>
	public static StreamPosition ConvertToLegacyStreamPosition(this StreamRevision revision) =>
		revision switch {
			_ when revision == StreamRevision.Unset => StreamPosition.Start,
			_ when revision == StreamRevision.Min   => StreamPosition.Start,
			_ when revision == StreamRevision.Max   => StreamPosition.End,
			_                                       => new StreamPosition((ulong)revision.Value)
		};

	/// <summary>
	/// Converts a <see cref="StreamPosition"/> instance to its corresponding <see cref="StreamRevision"/>.
	/// </summary>
	/// <param name="position">The <see cref="StreamPosition"/> to be converted.</param>
	/// <returns>The corresponding <see cref="StreamRevision"/> based on the provided <see cref="StreamPosition"/> value.</returns>
	public static StreamRevision ConvertToStreamRevision(this StreamPosition position) =>
		position switch {
			_ when position == StreamPosition.Start => StreamRevision.Min,
			_ when position == StreamPosition.End   => StreamRevision.Max,
			_ when position >= long.MaxValue        => StreamRevision.Max,
			_                                       => StreamRevision.From((long)position.ToUInt64())
		};

	/// <summary>
	/// Converts a <see cref="LogPosition"/> instance to its corresponding <see cref="Position"/>.
	/// </summary>
	/// <param name="position">The <see cref="LogPosition"/> to be converted.</param>
	/// <returns>The corresponding <see cref="Position"/> based on the provided <see cref="LogPosition"/> value.</returns>
	public static Position ConvertToLegacyPosition(this LogPosition position) =>
		position switch {
			_ when position == LogPosition.Unset    => Position.Start,
			_ when position == LogPosition.Earliest => Position.Start,
			_ when position == LogPosition.Latest   => Position.End,
			_                                       => new Position((ulong)position.Value, (ulong)position.Value)
		};

	/// <summary>
	/// Converts a <see cref="LogPosition"/> instance to its corresponding <see cref="FromAll"/> representation.
	/// </summary>
	/// <param name="position">The <see cref="LogPosition"/> to be converted.</param>
	/// <returns>A <see cref="FromAll"/> instance that represents the provided <see cref="LogPosition"/>.</returns>
	/// <returns>The corresponding <see cref="FromAll"/> position based on the provided <see cref="LogPosition"/> value.</returns>
	public static FromAll ConvertToLegacyFromAll(this LogPosition position) =>
		position switch {
			_ when position == LogPosition.Unset    => FromAll.Start,
			_ when position == LogPosition.Earliest => FromAll.Start,
			_ when position == LogPosition.Latest   => FromAll.End,
			_                                       => FromAll.After(new Position((ulong)position.Value, (ulong)position.Value))
		};

	/// <summary>
	/// Converts a <see cref="StreamRevision"/> instance to its corresponding <see cref="FromStream"/> representation.
	/// </summary>
	/// <param name="revision">The <see cref="StreamRevision"/> to be converted.</param>
	/// <returns>A <see cref="FromStream"/> instance that represents the provided <see cref="StreamRevision"/>.</returns>
	/// <returns>The corresponding <see cref="FromStream"/> position based on the provided <see cref="StreamRevision"/> value.</returns>
	public static FromStream ConvertToLegacyFromStream(this StreamRevision revision) =>
		revision switch {
			_ when revision == StreamRevision.Unset => FromStream.Start,
			_ when revision == StreamRevision.Min   => FromStream.Start,
			_ when revision == StreamRevision.Max   => FromStream.End,
			_                                       => FromStream.After(revision.ConvertToLegacyStreamPosition())
		};

	/// <summary>
	/// Converts a <see cref="Position"/> instance to its corresponding <see cref="LogPosition"/>.
	/// </summary>
	/// <param name="position">The <see cref="Position"/> to be converted.</param>
	/// <returns>The corresponding <see cref="LogPosition"/> based on the given <see cref="Position"/> value.</returns>
	public static LogPosition ConvertToLogPosition(this Position position) =>
		position switch {
			_ when position == Position.Start => LogPosition.Earliest,
			_ when position == Position.End   => LogPosition.Latest,
			_                                 => LogPosition.From((long)position.CommitPosition)
		};

	/// <summary>
	/// Converts a <see cref="Position"/> instance to its corresponding <see cref="LogPosition"/>.
	/// </summary>
	/// <param name="position">The <see cref="Position"/> to be converted.</param>
	/// <returns>The corresponding <see cref="LogPosition"/> based on the given <see cref="Position"/> value.</returns>
	public static LogPosition ConvertToLogPosition(this Position? position) =>
		position switch {
			null => LogPosition.Unset,
			_    => ConvertToLogPosition(position)
		};

	/// <summary>
	/// Converts a <see cref="ReadFilter"/> instance to its corresponding <see cref="IEventFilter"/>.
	/// </summary>
	/// <param name="filter">The <see cref="ReadFilter"/> to be converted.</param>
	/// <param name="checkpointInterval">The checkpoint interval value used in constructing the <see cref="IEventFilter"/>. Defaults to 1000.</param>
	/// <returns>An <see cref="IEventFilter"/> derived from the provided <see cref="ReadFilter"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the <see cref="ReadFilter"/> instance cannot be converted to a valid <see cref="IEventFilter"/>.</exception>
	public static IEventFilter ConvertToEventFilter(this ReadFilter filter, uint checkpointInterval = 1000) =>
		filter switch {
			{ IsEmptyFilter : true } => EventTypeFilter.None,
			{ IsStreamFilter: true } => StreamFilter.RegularExpression(filter.Expression, checkpointInterval),
			{ IsRecordFilter: true } => EventTypeFilter.RegularExpression(filter.Expression, checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid read filter.")
		};

	/// <summary>
	/// Converts a <see cref="ReadFilter"/> instance into its corresponding <see cref="IEventFilter"/> representation.
	/// </summary>
	/// <param name="filter">The <see cref="ReadFilter"/> to be converted.</param>
	/// <param name="checkpointInterval">The checkpoint interval used for the conversion, specified as an integer.</param>
	/// <returns>The corresponding <see cref="IEventFilter"/> based on the provided <see cref="ReadFilter"/>.</returns>
	public static IEventFilter ConvertToEventFilter(this ReadFilter filter, int checkpointInterval) =>
		ConvertToEventFilter(filter, (uint)checkpointInterval);

	/// <summary>
	/// Converts a <see cref="ReadFilter"/> instance to its corresponding <see cref="SubscriptionFilterOptions"/>.
	/// </summary>
	/// <param name="filter">The <see cref="ReadFilter"/> to be converted, determining the type of filtering to be applied.</param>
	/// <param name="checkpointInterval">The interval, in number of events, at which checkpoints are created. Defaults to 1000.</param>
	/// <returns>The corresponding <see cref="SubscriptionFilterOptions"/> based on the provided <see cref="ReadFilter"/>, or null if the filter is empty.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="filter"/> has an invalid type or scope.</exception>
	public static SubscriptionFilterOptions? ConvertToSubscriptionFilterOptions(this ReadFilter filter, uint checkpointInterval = 1000) =>
		filter switch {
			{ IsEmptyFilter : true } => null,
			{ IsStreamFilter: true } => new SubscriptionFilterOptions(StreamFilter.RegularExpression(filter.Expression, checkpointInterval), checkpointInterval),
			{ IsRecordFilter: true } => new SubscriptionFilterOptions(EventTypeFilter.RegularExpression(filter.Expression, checkpointInterval), checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid read filter.")
		};

	/// <summary>
	/// Converts a <see cref="ReadFilter"/> instance into an equivalent <see cref="SubscriptionFilterOptions"/> object.
	/// </summary>
	/// <param name="filter">The <see cref="ReadFilter"/> to be converted.</param>
	/// <param name="checkpointInterval">The checkpoint interval to use when creating the subscription filter options.</param>
	/// <returns>A <see cref="SubscriptionFilterOptions"/> instance derived from the provided <see cref="ReadFilter"/>; returns null if the filter is empty.</returns>
	public static SubscriptionFilterOptions? ConvertToSubscriptionFilterOptions(this ReadFilter filter, int checkpointInterval) =>
		ConvertToSubscriptionFilterOptions(filter, (uint)checkpointInterval);
}
