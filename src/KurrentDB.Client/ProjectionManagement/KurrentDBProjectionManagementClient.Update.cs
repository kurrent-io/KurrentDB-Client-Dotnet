using EventStore.Client;
using EventStore.Client.Projections;

namespace KurrentDB.Client {
	public partial class KurrentDBProjectionManagementClient {
		/// <summary>
		/// Updates a projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="emitEnabled"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task UpdateAsync(string name, string query, bool? emitEnabled = null,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			var options = new UpdateReq.Types.Options {
				Name = name,
				Query = query
			};
			if (emitEnabled.HasValue) {
				options.EmitEnabled = emitEnabled.Value;
			} else {
				options.NoEmitOptions = new Empty();
			}

			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new Projections.ProjectionsClient(
				channelInfo.CallInvoker).UpdateAsync(new UpdateReq {
				Options = options
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));

			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
