using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using KurrentDB.Protocol.Projections.V1;

namespace Kurrent.Client.Projections;

public partial class ProjectionsClient {
	/// <summary>
	/// Creates a one-time projection.
	/// </summary>
	/// <param name="query">The query that defines the projection.</param>
	/// <param name="cancellationToken">A token to observe for cancellation requests.</param>
	/// <returns>A result containing the success state or an error indicating why the creation failed.</returns>
	public async ValueTask<Result<Success, CreateOneTimeError>> CreateOneTime(string query, CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.CreateAsync(
				new CreateReq {
					Options = new CreateReq.Types.Options {
						OneTime = new Empty(),
						Query   = query
					}
				},
				cancellationToken: cancellationToken
			);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, CreateOneTimeError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateOneTimeError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateOneTime), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateOneTime), ex);
		}
	}

	/// <summary>
	/// Creates a continuous projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="query"></param>
	/// <param name="trackEmittedStreams"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, CreateContinuousError>> CreateContinuous(
		string name, string query, bool trackEmittedStreams = false, CancellationToken cancellationToken = default
	) {
		try {
			using var call = ServiceClient.CreateAsync(
				new CreateReq {
					Options = new CreateReq.Types.Options {
						Continuous = new CreateReq.Types.Options.Types.Continuous {
							Name                = name,
							TrackEmittedStreams = trackEmittedStreams
						},
						Query = query
					}
				},
				cancellationToken: cancellationToken
			);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, CreateContinuousError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateContinuousError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateContinuous), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateContinuous), ex);
		}
	}

	/// <summary>
	/// Creates a transient projection.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="query"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<Result<Success, CreateTransientError>> CreateTransient(string name, string query, CancellationToken cancellationToken = default) {
		try {
			using var call = ServiceClient.CreateAsync(
				new CreateReq {
					Options = new CreateReq.Types.Options {
						Transient = new CreateReq.Types.Options.Types.Transient {
							Name = name
						},
						Query = query
					}
				},
				cancellationToken: cancellationToken
			);

			await call.ResponseAsync.ConfigureAwait(false);

			return new Result<Success, CreateTransientError>();
		} catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
			return Result.Failure<Success, CreateTransientError>(
				ex switch {
					AccessDeniedException     => rpcEx.AsAccessDeniedError(),
					NotAuthenticatedException => rpcEx.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateTransient), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateTransient), ex);
		}
	}
}
