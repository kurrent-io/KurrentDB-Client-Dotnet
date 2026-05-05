using EventStore.Client;
using KurrentDB.Protocol.Projections.V1;
using static KurrentDB.Protocol.Projections.V1.Projections;

namespace KurrentDB.Client {
	public partial class KurrentDBProjectionManagementClient {
		/// <summary>
		/// Creates a one-time projection.
		/// </summary>
		/// <param name="query"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateOneTimeAsync(string query, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			ProjectionEngineVersion engineVersion = ProjectionEngineVersion.V1,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					OneTime       = new Empty(),
					Query         = query,
					EngineVersion = (int)engineVersion
				}
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a continuous projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="trackEmittedStreams"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>. <see cref="ProjectionEngineVersion.V2"/> does not support
		/// <paramref name="trackEmittedStreams"/>.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			ProjectionEngineVersion engineVersion = ProjectionEngineVersion.V1,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Continuous = new CreateReq.Types.Options.Types.Continuous {
						Name                = name,
						TrackEmittedStreams = trackEmittedStreams
					},
					Query         = query,
					EngineVersion = (int)engineVersion
				}
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a transient projection.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="query"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>.
		/// </param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task CreateTransientAsync(string name, string query, TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			ProjectionEngineVersion engineVersion = ProjectionEngineVersion.V1,
			CancellationToken cancellationToken = default) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			using var call = new ProjectionsClient(
				channelInfo.CallInvoker).CreateAsync(new CreateReq {
				Options = new CreateReq.Types.Options {
					Transient = new CreateReq.Types.Options.Types.Transient {
						Name = name
					},
					Query         = query,
					EngineVersion = (int)engineVersion
				}
			}, KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken));
			await call.ResponseAsync.ConfigureAwait(false);
		}
	}
}
