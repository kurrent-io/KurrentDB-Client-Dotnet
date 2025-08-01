// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using KurrentDB.Protocol.Users.V1;
using static KurrentDB.Protocol.Users.V1.UsersService;

namespace Kurrent.Client.Users;

public class UsersClient {
	internal UsersClient(KurrentClient source) =>
        ServiceClient = new UsersServiceClient(source.LegacyCallInvoker);

    UsersServiceClient ServiceClient { get; }

	public async ValueTask<Result<Success, ChangePasswordError>> ChangePassword(
		string loginName, string currentPassword, string newPassword, CancellationToken cancellationToken = default
	) {
		try {
			await ServiceClient.ChangePasswordAsync(
				new ChangePasswordReq {
					Options = new() {
						CurrentPassword = currentPassword,
						NewPassword     = newPassword,
						LoginName       = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, ChangePasswordError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, ChangePasswordError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(ChangePassword), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(ChangePassword), ex);
		}
	}

	public async ValueTask<Result<Success, CreateUserError>> CreateUser(
		string loginName, string fullName, string[] groups, string password,
		CancellationToken cancellationToken = default
	) {
		try {
			await ServiceClient
				.CreateAsync(
					new CreateReq {
						Options = new() {
							LoginName = loginName,
							FullName  = fullName,
							Password  = password,
							Groups    = { groups }
						}
					},
					cancellationToken: cancellationToken
				);

			return new Result<Success, CreateUserError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, CreateUserError>(
				ex.StatusCode switch {
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(CreateUser), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(CreateUser), ex);
		}
	}

	public async ValueTask<Result<Success, DeleteUserError>> DeleteUser(string loginName, CancellationToken cancellationToken = default) {
		try {
			await ServiceClient.DeleteAsync(
				new DeleteReq {
					Options = new() {
						LoginName = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, DeleteUserError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, DeleteUserError>(
				ex.StatusCode switch {
					StatusCode.NotFound         => ex.AsUserNotFoundError(),
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(DeleteUser), ex);
		}
	}

	public async ValueTask<Result<Success, DisableUserError>> DisableUser(string loginName, CancellationToken cancellationToken = default) {
		try {
			await ServiceClient.DisableAsync(
				new DisableReq {
					Options = new() {
						LoginName = loginName
					}
				}, cancellationToken: cancellationToken
			);

			return new Result<Success, DisableUserError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, DisableUserError>(
				ex.StatusCode switch {
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(DisableUser), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(DisableUser), ex);
		}
	}

	public async ValueTask<Result<Success, EnableUserError>> EnableUser(string loginName, CancellationToken cancellationToken = default) {
		try {
			await ServiceClient.EnableAsync(
				new EnableReq {
					Options = new() {
						LoginName = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, EnableUserError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, EnableUserError>(
				ex.StatusCode switch {
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex);
		}
	}

	public async ValueTask<Result<UserDetails, GetUserError>> GetUser(string loginName, CancellationToken cancellationToken = default) {
		try {
			return await ListAllCore(loginName, cancellationToken)
				.FirstAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		} catch (RpcException ex) {
			return Result.Failure<UserDetails, GetUserError>(
				ex.StatusCode switch {
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(GetUser), ex);
		}
	}

	public IAsyncEnumerable<UserDetails> ListAllAsync(CancellationToken cancellationToken = default) => ListAllCore(null, cancellationToken);

	IAsyncEnumerable<UserDetails> ListAllCore(string? loginName, CancellationToken cancellationToken = default) {
		var req = loginName is not null
			? new DetailsReq { Options = new() { LoginName = loginName } }
			: new DetailsReq();

		var call = ServiceClient.Details(req, cancellationToken: cancellationToken);

		return call.ResponseStream
			.ReadAllAsync(cancellationToken)
			.Select(static dr => new UserDetails {
					LoginName       = dr.UserDetails.LoginName,
					FullName        = dr.UserDetails.FullName,
					Groups          = dr.UserDetails.Groups.ToArray(),
					Disabled        = dr.UserDetails.Disabled,
					DateLastUpdated = dr.UserDetails.LastUpdated?.TicksSinceEpoch.FromTicksSinceEpoch()
				}
			);
	}

	public async ValueTask<Result<Success, ResetPasswordError>> ResetPassword(
		string loginName, string newPassword, CancellationToken cancellationToken = default
	) {
		try {
			await ServiceClient.ResetPasswordAsync(
				new ResetPasswordReq {
					Options = new() {
						NewPassword = newPassword,
						LoginName   = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, ResetPasswordError>();
		} catch (RpcException ex) {
			return Result.Failure<Success, ResetPasswordError>(
				ex.StatusCode switch {
					StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
					StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
					_                           => throw KurrentClientException.CreateUnknown(nameof(ResetPassword), ex)
				}
			);
		} catch (Exception ex) {
			throw KurrentClientException.CreateUnknown(nameof(ResetPassword), ex);
		}
	}
}
