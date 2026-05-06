using EventStore.Client;
using KurrentDB.Protocol.Projections.V1;
using static KurrentDB.Protocol.Projections.V1.Projections;

namespace KurrentDB.Client {
	public partial class KurrentDBProjectionManagementClient {
		/// <summary>
		/// Creates a one-time projection.
		/// </summary>
		/// <param name="query">The JavaScript source of the projection.</param>
		/// <param name="deadline">The maximum time to wait for the operation to complete.</param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> to use for the operation.</param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>.
		/// </param>
		/// <param name="cancellationToken">The token used to cancel the operation.</param>
		/// <returns>A <see cref="Task"/> that completes when the projection has been created.</returns>
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
		/// <param name="name">The name of the projection.</param>
		/// <param name="query">The JavaScript source of the projection.</param>
		/// <param name="trackEmittedStreams">Whether the streams emitted by this projection should be tracked.</param>
		/// <param name="deadline">The maximum time to wait for the operation to complete.</param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> to use for the operation.</param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>. <see cref="ProjectionEngineVersion.V2"/> does not support
		/// <paramref name="trackEmittedStreams"/>.
		/// </param>
		/// <param name="cancellationToken">The token used to cancel the operation.</param>
		/// <returns>A <see cref="Task"/> that completes when the projection has been created.</returns>
		public async Task CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			ProjectionEngineVersion engineVersion = ProjectionEngineVersion.V1,
			CancellationToken cancellationToken = default) {
			if (engineVersion == ProjectionEngineVersion.V2 && trackEmittedStreams)
				throw new ArgumentException(
					$"{nameof(trackEmittedStreams)} is not supported when {nameof(engineVersion)} is {nameof(ProjectionEngineVersion.V2)}.",
					nameof(trackEmittedStreams));

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
		/// <param name="name">The name of the projection.</param>
		/// <param name="query">The JavaScript source of the projection.</param>
		/// <param name="deadline">The maximum time to wait for the operation to complete.</param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> to use for the operation.</param>
		/// <param name="engineVersion">
		/// The projection engine version to use. The engine version is pinned at create time and cannot be changed later.
		/// Defaults to <see cref="ProjectionEngineVersion.V1"/>.
		/// </param>
		/// <param name="cancellationToken">The token used to cancel the operation.</param>
		/// <returns>A <see cref="Task"/> that completes when the projection has been created.</returns>
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
