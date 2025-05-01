using System.Runtime.CompilerServices;
using KurrentDB.Client.Model;

namespace KurrentDB.Client;

public record LegacyAppendOptions {
	/// <summary>
	/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
	/// </summary>
	public bool ThrowOnAppendFailure { get; init; }

	/// <summary>
	/// The batch size, in bytes.
	/// </summary>
	public int BatchAppendSize { get; init; }

	/// <summary>
	/// A callback function to extract the authorize header value from the <see cref="UserCredentials"/> used in the operation.
	/// </summary>
	public Func<UserCredentials, CancellationToken, ValueTask<string>> GetAuthenticationHeaderValue { get; init; } = null!;

	public TimeSpan?        Deadline        { get; init; } = null;
	public UserCredentials? UserCredentials { get; init; } = null;
}

public record LegacyReadOptions {
	/// <summary>
	///
	/// </summary>
	public bool ResolveLinkTos { get; init; }

	public TimeSpan?        Deadline        { get; init; } = null;
	public UserCredentials? UserCredentials { get; init; } = null;
}

public static partial class KurrentDBClientExtensions {
	static LegacyConverters LegacyConverters { get; set; } = null!;

	#region . Append Operations .

	public record AppendStreamResult {
		public long Position       { get; set; }
		public long StreamRevision { get; set; }
	}

	public static async Task<AppendStreamResult> AppendStream(
		this KurrentDBClient client,
		string streamName,
		StreamState expectedState,
		IEnumerable<Message> messages,
		Func<LegacyAppendOptions, LegacyAppendOptions>? configureOptions = null,
		CancellationToken cancellationToken = default
	) {
		var legacyOptions = configureOptions?.Invoke(new LegacyAppendOptions());

		var eventData = await LegacyConverters.ConvertMessagesToEventDataAsync(
			streamName,
			messages,
			metadata => {
				// metadata.Set(HeaderKeys.ProducerId, "");
				// metadata.Set(HeaderKeys.ProducerRequestId, "");
			},
			cancellationToken
		).ToArrayAsync(cancellationToken);

		var result = await client
			.AppendToStreamAsync(streamName, expectedState, eventData, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return new AppendStreamResult {
			Position       = (long)result.LogPosition.CommitPosition,
			StreamRevision = result.NextExpectedStreamState.ToInt64()
		};
	}

	#endregion

	#region . Read Operations .

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

			var record = await LegacyConverters
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

			var record = await LegacyConverters
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
				? await LegacyConverters
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
				? await LegacyConverters
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
				? await LegacyConverters
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

	#endregion

	#region . Consume Operations .

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

	#endregion
}
