using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using KurrentDB.Client.Model;
using OneOf;

namespace KurrentDB.Client;

[PublicAPI]
public static class ConsumeMessages {
	public record Checkpoint(long Position, DateTimeOffset Timestamp);

	public record CaughtUp(long Position, DateTimeOffset Timestamp, long? StreamRevision = null) {
		public bool HasStreamRevision => StreamRevision.HasValue;
	}

	public record FellBehind(long Position, DateTimeOffset Timestamp, long? StreamRevision = null) {
		public bool HasStreamRevision => StreamRevision.HasValue;
	}
}

[GenerateOneOf]
public partial class SubscribeResult : OneOfBase<Record, ConsumeMessages.Checkpoint, ConsumeMessages.CaughtUp, ConsumeMessages.FellBehind>;


#pragma warning disable CS8509
public static class KurrentDBClientSubscriber {
	public static async IAsyncEnumerable<SubscribeResult> Subscribe(
		this KurrentDBClient client, Position position, ConsumeFilter filter,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	) {
		if (filter.IsStreamNameFilter) {
			FromStream start;
			if (position == Position.Start)
				start = FromStream.Start;
			else if (position == Position.End)
				start = FromStream.End;
			else {
				start = FromStream.After(await client
					.GetStreamPosition(position, cancellationToken)
					.ConfigureAwait(false));
			}

			await using var result = client.SubscribeToStream(filter.Expression, start, cancellationToken: cancellationToken);


			string subscriptionId;

			await foreach (var msg in result.Messages.WithCancellation(cancellationToken)) {
				if (cancellationToken.IsCancellationRequested)
					break;

				if (msg is StreamMessage.SubscriptionConfirmation confirmation) {
					subscriptionId = confirmation.SubscriptionId;
					continue;
				}

				// if (msg is StreamMessage.StreamCheckpointReached checkpoint)
				// 	yield return new ConsumeMessages.Checkpoint((long)checkpoint.Position.CommitPosition, checkpoint.Timestamp);

				// if (msg is StreamMessage.CaughtUp legacyCaughtUp) {
				//
				// 	long streamRevision;
				//
				// 	if (!legacyCaughtUp.HasStreamRevision) {
				// 		var streamRevision = await client
				// 			.GetStreamPosition(legacyCaughtUp.Position, cancellationToken)
				// 			.ConfigureAwait(false);
				// 		yield return new ConsumeMessages.CaughtUp((long)legacyCaughtUp.Position.CommitPosition, legacyCaughtUp.Timestamp, legacyCaughtUp.StreamRevision);
				// 	}
				// 	else {
				// 		yield return  new ConsumeMessages.CaughtUp((long)legacyCaughtUp.Position.CommitPosition, legacyCaughtUp.Timestamp, legacyCaughtUp.StreamRevision);
				// 	}
				// }

				// if (msg is StreamMessage.FellBehind fellBehind) {
				// 	yield return new ConsumeMessages.FellBehind((long)fellBehind.Position.CommitPosition, fellBehind.Timestamp, fellBehind.StreamRevision);
				// }

				if (msg is not StreamMessage.Event evt)
					continue;

				var record = await client.LegacyMapper
					.ConvertResolvedEventToRecordAsync(evt.ResolvedEvent, cancellationToken)
					.ConfigureAwait(false);

				yield return record;
			}
		}
		else {
			// var filterOptions = filter.IsEmptyFilter ? null : new SubscriptionFilterOptions(filter.ToEventFilter());
			//
			// await using var result = client.SubscribeToAll(
			// 	FromAll.After(startPosition),
			// 	filterOptions: filterOptions,
			// 	cancellationToken: cancellationToken
			// );
			//
			// await foreach (var msg in result.Messages.WithCancellation(cancellationToken)) {
			// 	if (cancellationToken.IsCancellationRequested)
			// 		break;
			//
			// 	if (msg is StreamMessage.AllStreamCheckpointReached checkpoint)
			// 		yield return new ConsumeMessages.Checkpoint((long)checkpoint.Position.CommitPosition, checkpoint.Timestamp);
			//
			// 	if (msg is StreamMessage.CaughtUp caughtUp)
			// 		yield return new ConsumeMessages.CaughtUp((long)caughtUp.Position.CommitPosition, caughtUp.Timestamp, caughtUp.StreamRevision);
			//
			// 	if (msg is StreamMessage.FellBehind fellBehind)
			// 		yield return new ConsumeMessages.FellBehind((long)fellBehind.Position.CommitPosition, fellBehind.Timestamp, fellBehind.StreamRevision);
			//
			// 	if (msg is not StreamMessage.Event evt)
			// 		continue;
			//
			// 	var record = await client.LegacyMapper
			// 		.ConvertResolvedEventToRecordAsync(evt.ResolvedEvent, cancellationToken)
			// 		.ConfigureAwait(false);
			//
			// 	yield return record;
			// }
		}
	}
}
