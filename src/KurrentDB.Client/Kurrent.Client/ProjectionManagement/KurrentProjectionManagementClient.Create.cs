using EventStore.Client;
using EventStore.Client.Projections;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	/// <summary>
	/// Creates a one-time projection.
	/// </summary>
	/// <param name="query"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask CreateOneTimeAsync(string query, CancellationToken cancellationToken = default) {
		using var call = ServiceClient.CreateAsync(
			new CreateReq {
				Options = new CreateReq.Types.Options {
					OneTime = new Empty(),
					Query   = query
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a continuous projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="query"></param>
	/// <param name="trackEmittedStreams"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask CreateContinuousAsync(string name, string query, bool trackEmittedStreams = false, CancellationToken cancellationToken = default) {
		using var call = ServiceClient.CreateAsync(
			new CreateReq {
				Options = new CreateReq.Types.Options {
					Continuous = new CreateReq.Types.Options.Types.Continuous {
						Name                = name,
						TrackEmittedStreams = trackEmittedStreams
					},
					Query = query
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Creates a transient projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="query"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask CreateTransientAsync(string name, string query, CancellationToken cancellationToken = default) {
		using var call = ServiceClient.CreateAsync(
			new CreateReq {
				Options = new CreateReq.Types.Options {
					Transient = new CreateReq.Types.Options.Types.Transient {
						Name = name
					},
					Query = query
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}
}
