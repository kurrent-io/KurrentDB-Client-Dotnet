using System.Runtime.CompilerServices;
using KurrentDB.Client.Model;
using KurrentDB.Client.Schema.Serialization;

namespace KurrentDB.Client;


public record LegacyReadOptions {
	/// <summary>
	///
	/// </summary>
	public bool ResolveLinkTos { get; init; }

	public TimeSpan?        Deadline        { get; init; } = null;
	public UserCredentials? UserCredentials { get; init; } = null;
}


#pragma warning disable CS8509
public static class KurrentDBClientReader {
	static LegacyProtocolMapper LegacyMapper { get; set; } = null!;
	static ISchemaSerializer        SchemaSerializer { get; set; } = null!;
	static IMetadataDecoder         MetadataDecoder  { get; set; } = null!;

	public static async IAsyncEnumerable<Record> ReadAllStream(
		this KurrentDBClient client,
		Position position,
		int maxCount,
		ConsumeFilter? filter = null,
		Direction direction = Direction.Forwards,
		Func<LegacyReadOptions, LegacyReadOptions>? configureOptions = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var legacyOptions = configureOptions?.Invoke(new LegacyReadOptions());

		var result = client.ReadAllAsync(direction, position, filter?.ToEventFilter(), maxCount, cancellationToken: cancellationToken);

		await foreach (var msg in result.Messages.WithCancellation(cancellationToken)) {
			if (cancellationToken.IsCancellationRequested)
				break;

			if (msg is StreamMessage.LastAllStreamPosition)
				break;

			if (msg is not StreamMessage.Event evt)
				continue;

			//TODO SS: what to do with the other possible messages?

			var record = await LegacyMapper
				.ConvertResolvedEventToRecordAsync(evt.ResolvedEvent, cancellationToken)
				.ConfigureAwait(false);

			yield return record;
		}
	}

	public static async IAsyncEnumerable<Record> ReadStream(
		this KurrentDBClient client,
		string streamName,
		StreamPosition revision,
		long maxCount = long.MaxValue,
		ConsumeFilter? filter = null,
		Direction direction = Direction.Forwards,
		Func<LegacyReadOptions, LegacyReadOptions>? configureOptions = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		var legacyOptions = configureOptions?.Invoke(new LegacyReadOptions());

		var result = client.ReadStreamAsync(direction, streamName, revision, maxCount, cancellationToken: cancellationToken);

		await foreach (var msg in result.Messages.WithCancellation(cancellationToken)) {
			if (cancellationToken.IsCancellationRequested)
				break;

			if (msg is StreamMessage.LastAllStreamPosition)
				break;

			if (msg is not StreamMessage.Event evt)
				continue;

			//TODO SS: what to do with the other possible messages?

			var record = await LegacyMapper
				.ConvertResolvedEventToRecordAsync(evt.ResolvedEvent, cancellationToken)
				.ConfigureAwait(false);

			yield return record;
		}
	}

	public static async ValueTask<Record> ReadFirstStreamRecord(
		this KurrentDBClient client, string stream, CancellationToken cancellationToken = default) {
		try {
			var result = client.ReadStreamAsync(
				direction: Direction.Forwards,
				streamName: stream,
				revision: StreamPosition.Start,
				maxCount: 1,
				cancellationToken: cancellationToken
			);

			ResolvedEvent? re = await result
				.FirstOrDefaultAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return re?.Event != null
				? await LegacyMapper
					.ConvertResolvedEventToRecordAsync(re.Value, cancellationToken)
					.ConfigureAwait(false)
				: Record.None;
		}
		catch (StreamNotFoundException) {
			return Record.None;
		}
		catch (StreamDeletedException) { // tombstoned
			return Record.None;
		}
	}

	public static async ValueTask<Record> ReadLastStreamRecord(
		this KurrentDBClient client, string stream, CancellationToken cancellationToken = default) {
		try {
			var result = client.ReadStreamAsync(
				direction: Direction.Backwards,
				streamName: stream,
				revision: StreamPosition.End,
				maxCount: 1,
				cancellationToken: cancellationToken
			);

			ResolvedEvent? re = await result
				.FirstOrDefaultAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return re?.Event is not null
				? await LegacyMapper
					.ConvertResolvedEventToRecordAsync(re.Value, cancellationToken)
					.ConfigureAwait(false)
				: Record.None;
		}
		catch (StreamNotFoundException) {
			return Record.None;
		}
		catch (StreamDeletedException) { // tombstoned
			return Record.None;
		}
	}

	public static async ValueTask<Record> ReadRecord(this KurrentDBClient client, Position position, CancellationToken cancellationToken = default) {
		try {
			ResolvedEvent? re = await client
				.ReadAllAsync(Direction.Forwards, position, maxCount: 1, cancellationToken: cancellationToken)
				.FirstOrDefaultAsync(cancellationToken);

			return re?.Event is not null
				? await LegacyMapper
					.ConvertResolvedEventToRecordAsync(re.Value, cancellationToken)
					.ConfigureAwait(false)
				: Record.None;
		}
		catch (StreamNotFoundException) {
			return Record.None;
		}
		catch (StreamDeletedException) { // tombstoned
			return Record.None;
		}
	}

	static async ValueTask<long?> GetStreamRevision(this KurrentDBClient client, Position position, CancellationToken cancellationToken = default) {
		try {
			ResolvedEvent? re = await client
				.ReadAllAsync(Direction.Forwards, position, maxCount: 1, cancellationToken: cancellationToken)
				.FirstOrDefaultAsync(cancellationToken);

			return re?.Event is not null
				? re.Value.OriginalEventNumber.ToInt64()
				: null;
		}
		catch (StreamNotFoundException) {
			return null;
		}
		catch (StreamDeletedException) { // tombstoned
			return null;
		}
	}
}
