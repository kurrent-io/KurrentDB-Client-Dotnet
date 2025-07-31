using EventStore.Client;
using EventStore.Client.Projections;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.Model.ProjectionManagement;
using KurrentDB.Client;

namespace Kurrent.Client;

/// <summary>
/// Provides functionality to manage projections in the Kurrent system.
/// </summary>
public partial class KurrentProjectionsClient {
	/// <summary>
	/// Enables a projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, EnableError>> Enable(string name, CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.EnableAsync(
				new EnableReq {
					Options = new EnableReq.Types.Options {
						Name = name
					}
				},
				cancellationToken: cancellationToken
			);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, EnableError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, EnableError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Enable), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Enable), ex);
		}
	}

	/// <summary>
	/// Resets a projection. This will re-emit events. Streams that are written to from the projection will also be soft deleted.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, ResetError>> Reset(string name, CancellationToken cancellationToken = default) {
		try {
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

			return new Result<Success, ResetError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, ResetError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Reset), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Reset), ex);
		}
	}

	/// <summary>
	/// Aborts a projection. Does not save the projection's checkpoint.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, AbortError>> Abort(string name, CancellationToken cancellationToken = default) {
		try {
			await DisableInternal(name, false, cancellationToken).ConfigureAwait(false);

			return new Result<Success, AbortError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, AbortError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Abort), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Abort), ex);
		}
	}

	/// <summary>
	/// Disables a projection. Saves the projection's checkpoint.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, DisableError>> Disable(string name, CancellationToken cancellationToken = default) {
		try {
			await DisableInternal(name, true, cancellationToken).ConfigureAwait(false);

			return new Result<Success, DisableError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, DisableError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Abort), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Abort), ex);
		}
	}

	public async ValueTask<Result<Success, RestartSubsystemError>> RestartSubsystem(CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.RestartSubsystemAsync(new Empty(), cancellationToken: cancellationToken);
			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, RestartSubsystemError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, RestartSubsystemError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(Abort), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(Abort), ex);
		}
	}

	async ValueTask DisableInternal(string name, bool writeCheckpoint, CancellationToken cancellationToken) {
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
