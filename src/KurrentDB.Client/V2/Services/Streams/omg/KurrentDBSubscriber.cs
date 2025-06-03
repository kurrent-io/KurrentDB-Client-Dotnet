// using System.Runtime.CompilerServices;
// using KurrentDB.Client.Model;
// using OneOf;
//
// namespace KurrentDB.Client;
//
// [PublicAPI]
// [GenerateOneOf]
// public partial class SubscribeResult : OneOfBase<Record, Heartbeat> {
// 	public bool IsRecord    => IsT0;
// 	public bool IsHeartbeat => IsT1;
//
// 	public Record    AsRecord()    => AsT0;
// 	public Heartbeat AsHeartbeat() => AsT1;
// }
//
// public static class KurrentDBClientSubscriber {
// 	public static IAsyncEnumerable<SubscribeResult> UnifiedSubscribe(
// 		this KurrentDBClient client,
// 		LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
// 		CancellationToken cancellationToken = default
// 	) {
// 		var session = filter.IsStreamNameFilter
// 			? client.SubscribeToStream(
// 				filter.Expression, startPosition, filter,
// 				cancellationToken: cancellationToken
// 			)
// 			: client.SubscribeToAll(
// 				startPosition, filter, heartbeatOptions,
// 				cancellationToken: cancellationToken
// 			);
//
// 		return session;
// 	}
//
// 	public static async IAsyncEnumerable<SubscribeResult> SubscribeToAll(
// 		this KurrentDBClient client,
// 		LogPosition startPosition, ReadFilter filter, HeartbeatOptions heartbeatOptions,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var start       = startPosition.ConvertToLegacyFromAll();
// 		var eventFilter = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
//
// 		// wth?!?... is SubscriptionFilterOptions.CheckpointInterval != IEventFilter.MaxSearchWindow ?!?!?!
// 		var filterOptions = new SubscriptionFilterOptions(eventFilter, (uint)heartbeatOptions.RecordsThreshold);
//
// 		await using var session = client.SubscribeToAll(
// 			start: start,
// 			filterOptions: filterOptions,
// 			cancellationToken: cancellationToken
// 		);
//
// 		await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false)) {
// 			switch (msg) {
// 				case StreamMessage.Event { ResolvedEvent: var re }:
// 					var record = await client.DataConverter
// 						.ConvertToRecord(re, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					yield return record;
// 					break;
//
// 				case StreamMessage.AllStreamCheckpointReached checkpoint: {
// 					var heartbeat = Heartbeat.CreateCheckpoint(
// 						checkpoint.Position.ConvertToLogPosition(),
// 						checkpoint.Timestamp);
//
// 					yield return heartbeat;
// 					break;
// 				}
//
// 				case StreamMessage.CaughtUp caughtUp: {
// 					var heartbeat = Heartbeat.CreateCaughtUp(
// 						caughtUp.Position.ConvertToLogPosition(),
// 						caughtUp.Timestamp);
//
// 					yield return heartbeat;
// 					break;
// 				}
//
// 				// new protocol, new model and this? thi is just noise
// 				// case StreamMessage.FellBehind fellBehind:
// 				// case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
// 				// case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
// 				// 	break;
// 			}
// 		}
// 	}
//
// 	public static async IAsyncEnumerable<SubscribeResult> SubscribeToStream(
// 		this KurrentDBClient client,
// 		string stream, StreamRevision startRevision, ReadFilter filter,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var start = startRevision.ConvertToLegacyFromStream();
//
// 		await using var session = client.SubscribeToStream(
// 			streamName: stream,
// 			start: start,
// 			cancellationToken: cancellationToken
// 		);
//
// 		await foreach (var msg in session.Messages.WithCancellation(cancellationToken).ConfigureAwait(false)) {
// 			switch (msg) {
// 				case StreamMessage.Event { ResolvedEvent: var re }:
// 					var record = await client.DataConverter
// 						.ConvertToRecord(re, cancellationToken)
// 						.ConfigureAwait(false);
//
// 					yield return record;
//
// 					// FILTER ALERT!
// 					// for now we could apply the filter locally until we refactor the server operation.
//
// 					// if (filter.IsEmptyFilter)
// 					// 	yield return record;
// 					// else {
// 					// 	switch (filter.Scope) {
// 					// 		case ReadFilterScope.Stream:
// 					// 			if (filter.IsMatch(record.Stream))
// 					// 				yield return record;
// 					// 			break;
// 					//
// 					// 		case ReadFilterScope.SchemaName:
// 					// 			if (filter.IsMatch(record.Schema.SchemaName))
// 					// 				yield return record;
// 					// 			break;
// 					//
// 					// 		// case ReadFilterScope.Properties:
// 					// 		// 	if (filter.IsMatch(record.Metadata))
// 					// 		// 		yield return record;
// 					// 		// 	break;
// 					//
// 					// 		// case ReadFilterScope.Record:
// 					// 		// 	if (filter.IsMatch(record.Schema.SchemaName))
// 					// 		// 		yield return record;
// 					// 		// 	break;
// 					//
// 					// 		// default:
// 					// 		// 	// if no scope is specified, we assume the filter applies to both stream and record
// 					// 		// 	if (filter.IsStreamNameFilter && filter.IsMatch(record.Stream) ||
// 					// 		// 	    filter.IsRecordFilter && filter.IsMatch(record.Data.Span))
// 					// 		// 		yield return record;
// 					// 		// 	break;
// 					//
// 					// 	}
// 					// }
//
// 					break;
//
// 				// its the same message as in SubscribeToAll, still need to test it...
// 				case StreamMessage.AllStreamCheckpointReached checkpoint: {
// 					var heartbeat = Heartbeat.CreateCheckpoint(
// 						checkpoint.Position.ConvertToLogPosition(),
// 						checkpoint.Timestamp);
//
// 					yield return heartbeat;
// 					break;
// 				}
//
// 				case StreamMessage.CaughtUp caughtUp: {
// 					var heartbeat = Heartbeat.CreateCaughtUp(
// 						caughtUp.Position.ConvertToLogPosition(),
// 						caughtUp.Timestamp);
//
// 					yield return heartbeat;
// 					break;
// 				}
//
// 				case StreamMessage.NotFound:
// 					throw new StreamNotFoundException(stream);
//
// 				// new protocol, new model and this? thi is just noise
// 				// case StreamMessage.FellBehind fellBehind:
// 				// case StreamMessage.LastAllStreamPosition lastAllStreamPosition:
// 				// case StreamMessage.SubscriptionConfirmation subscriptionConfirmation:
// 				// 	break;
// 			}
// 		}
// 	}
//
// 	public static async IAsyncEnumerable<SubscribeResult> SubscribeToStream(
// 		this KurrentDBClient client,
// 		string stream, LogPosition startPosition, ReadFilter filter,
// 		[EnumeratorCancellation] CancellationToken cancellationToken = default
// 	) {
// 		var revision = startPosition switch {
// 			_ when startPosition == LogPosition.Unset    => StreamRevision.Min,
// 			_ when startPosition == LogPosition.Earliest => StreamRevision.Min,
// 			_ when startPosition == LogPosition.Latest   => StreamRevision.Max,
// 			_                                            => await client.GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
// 		};
//
// 		var session = client.SubscribeToStream(stream, revision, filter, cancellationToken);
// 		await foreach (var record in session.ConfigureAwait(false))
// 			yield return record;
// 	}
// }
