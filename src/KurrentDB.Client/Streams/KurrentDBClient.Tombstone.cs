using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
		/// </summary>
		/// <param name="streamName">The name of the stream to tombstone.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="options">Optional settings for the tombstone operation, e.g. deadline, user credentials etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> TombstoneAsync(
			string streamName,
			StreamState expectedState,
			TombstoneOptions? options = null,
			CancellationToken cancellationToken = default
		) =>
			TombstoneInternal(
				new TombstoneReq {
					Options = new TombstoneReq.Types.Options {
						StreamIdentifier = streamName
					}
				}.WithAnyStreamRevision(expectedState),
				options,
				cancellationToken
			);

		async Task<DeleteResult> TombstoneInternal(
			TombstoneReq request,
			TombstoneOptions? options,
			CancellationToken cancellationToken
		) {
			_log.LogDebug("Tombstoning stream {streamName}.", request.Options.StreamIdentifier);

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Streams.StreamsClient(channelInfo.CallInvoker).TombstoneAsync(
				request,
				KurrentDBCallOptions.CreateNonStreaming(
					Settings,
					options?.Deadline,
					options?.UserCredentials,
					cancellationToken
				)
			);

			var result = await call.ResponseAsync.ConfigureAwait(false);

			return new DeleteResult(new Position(result.Position.CommitPosition, result.Position.PreparePosition));
		}
	}

	[Obsolete("Those extensions may be removed in the future versions", false)]
	public static class ObsoleteKurrentDBClientTombstoneExtensions {
		/// <summary>
		/// Tombstones a stream asynchronously. Note: Tombstoned streams can never be recreated.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to tombstone.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with TombstoneOptions parameter",
			false
		)]
		public static Task<DeleteResult> TombstoneAsync(
			KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.TombstoneAsync(
				streamName,
				expectedState,
				new TombstoneOptions { Deadline = deadline, UserCredentials = userCredentials },
				cancellationToken
			);
	}

	public class TombstoneOptions : OperationOptions;
}
