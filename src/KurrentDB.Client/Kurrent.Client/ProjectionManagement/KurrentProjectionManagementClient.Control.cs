using EventStore.Client;
using EventStore.Client.Projections;

namespace Kurrent.Client;

public partial class KurrentProjectionManagementClient {
	/// <summary>
	/// Enables a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task EnableAsync(string name, CancellationToken cancellationToken = default) {
		using var call = ServiceClient.EnableAsync(
			new EnableReq {
				Options = new EnableReq.Types.Options {
					Name = name
				}
			},
			cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Resets a projection. This will re-emit events. Streams that are written to from the projection will also be soft deleted.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task ResetAsync(string name, CancellationToken cancellationToken = default) {
		using var call = ServiceClient.ResetAsync(
			new ResetReq {
				Options = new ResetReq.Types.Options {
					Name            = name,
					WriteCheckpoint = true
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}

	/// <summary>
	/// Aborts a projection. Does not save the projection's checkpoint.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task AbortAsync(string name, CancellationToken cancellationToken = default) => DisableInternalAsync(name, false, cancellationToken);

	/// <summary>
	/// Disables a projection. Saves the projection's checkpoint.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public Task DisableAsync(string name, CancellationToken cancellationToken = default) =>
		DisableInternalAsync(name, true, cancellationToken);

	/// <summary>
	/// Restarts the projection subsystem.
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task RestartSubsystemAsync(CancellationToken cancellationToken = default) {
		using var call = ServiceClient.RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken);
		await call.ResponseAsync.ConfigureAwait(false);
	}

	async Task DisableInternalAsync(string name, bool writeCheckpoint, CancellationToken cancellationToken) {
		using var call = ServiceClient.DisableAsync(
			new DisableReq {
				Options = new DisableReq.Types.Options {
					Name            = name,
					WriteCheckpoint = writeCheckpoint
				}
			}
		  , cancellationToken: cancellationToken
		);

		await call.ResponseAsync.ConfigureAwait(false);
	}
}
