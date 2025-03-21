using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Deletes a stream asynchronously.
		/// </summary>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="options">Optional settings for the delete operation, e.g. deadline, user credentials etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<DeleteResult> DeleteAsync(
			string streamName,
			StreamState expectedState,
			DeleteOptions? options = null,
			CancellationToken cancellationToken = default
		) =>
			DeleteInternal(
				new DeleteReq {
					Options = new DeleteReq.Types.Options {
						StreamIdentifier = streamName
					}
				}.WithAnyStreamRevision(expectedState),
				options,
				cancellationToken
			);

		async Task<DeleteResult> DeleteInternal(
			DeleteReq request,
			DeleteOptions? options,
			CancellationToken cancellationToken
		) {
			_log.LogDebug("Deleting stream {streamName}.", request.Options.StreamIdentifier);
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Streams.StreamsClient(channelInfo.CallInvoker).DeleteAsync(
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

	public class DeleteOptions : OperationOptions;

	public static class KurrentDBClientObsoleteDeleteExtensions {
		/// <summary>
		/// Deletes a stream asynchronously.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to delete.</param>
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream being deleted.</param>
		/// <param name="deadline">The maximum time to wait before terminating the call.</param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with DeleteOptions parameter",
			false
		)]
		public static Task<DeleteResult> DeleteAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.DeleteAsync(
				streamName,
				expectedState,
				new DeleteOptions{ Deadline = deadline, UserCredentials = userCredentials },
				cancellationToken
			);
	}
}
