using EventStore.Client.Users;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;
using UserDetails = Kurrent.Client.Model.UserDetails;

namespace Kurrent.Client;

public class KurrentUserManagementClient {
	internal KurrentUserManagementClient(CallInvoker callInvoker, KurrentClientOptions options) {
		Options       = options;
		ServiceClient = new Users.UsersClient(callInvoker);
	}

	internal KurrentClientOptions Options { get; }

	Users.UsersClient ServiceClient { get; }

	public async ValueTask<Result<Success, ChangePasswordError>> ChangePassword(
		string loginName, string currentPassword, string newPassword, CancellationToken cancellationToken = default
	) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);
		ArgumentException.ThrowIfNullOrEmpty(currentPassword);
		ArgumentException.ThrowIfNullOrEmpty(newPassword);

		try {
			await ServiceClient.ChangePasswordAsync(
				new ChangePasswordReq {
					Options = new ChangePasswordReq.Types.Options {
						CurrentPassword = currentPassword,
						NewPassword     = newPassword,
						LoginName       = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, ChangePasswordError>();
		} catch (Exception ex) {
			return Result.Failure<Success, ChangePasswordError>(
				ex switch {
					AccessDeniedException => ex.AsAccessDeniedError(),
					_                     => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
				}
			);
		}
	}

	public async ValueTask<Result<Success, CreateUserError>> CreateUser(
		string loginName, string fullName, string[] groups, string password,
		CancellationToken cancellationToken = default
	) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);
		ArgumentException.ThrowIfNullOrEmpty(fullName);
		ArgumentException.ThrowIfNullOrEmpty(password);
		ArgumentNullException.ThrowIfNull(groups);

		try {
			await ServiceClient
				.CreateAsync(
					new CreateReq {
						Options = new CreateReq.Types.Options {
							LoginName = loginName,
							FullName  = fullName,
							Password  = password,
							Groups    = { groups }
						}
					},
					cancellationToken: cancellationToken
				);

			return new Result<Success, CreateUserError>();
		} catch (Exception ex) {
			return Result.Failure<Success, CreateUserError>(
				ex switch {
					AccessDeniedException     => ex.AsAccessDeniedError(),
					NotAuthenticatedException => ex.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(CreateUser), ex)
				}
			);
		}
	}

	public async ValueTask<Result<Success, DeleteUserError>> DeleteUser(string loginName, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);

		try {
			await ServiceClient.DeleteAsync(
				new DeleteReq {
					Options = new DeleteReq.Types.Options {
						LoginName = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, DeleteUserError>();
		} catch (Exception ex) {
			return Result.Failure<Success, DeleteUserError>(
				ex switch {
					UserNotFoundException     => ex.AsUserNotFoundError(),
					NotAuthenticatedException => ex.AsNotAuthenticatedError(),
					AccessDeniedException     => ex.AsAccessDeniedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
				}
			);
		}
	}

	public async ValueTask<Result<Success, DisableUserError>> DisableUser(string loginName, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);

		try {
			await ServiceClient.DisableAsync(
				new DisableReq {
					Options = new DisableReq.Types.Options {
						LoginName = loginName
					}
				}, cancellationToken: cancellationToken
			);

			return new Result<Success, DisableUserError>();
		} catch (Exception ex) {
			return Result.Failure<Success, DisableUserError>(
				ex switch {
					AccessDeniedException     => ex.AsAccessDeniedError(),
					NotAuthenticatedException => ex.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
				}
			);
		}
	}

	public async ValueTask<Result<Success, EnableUserError>> EnableUser(string loginName, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);

		try {
			await ServiceClient.EnableAsync(
				new EnableReq {
					Options = new EnableReq.Types.Options {
						LoginName = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, EnableUserError>();
		} catch (Exception ex) {
			return Result.Failure<Success, EnableUserError>(
				ex switch {
					AccessDeniedException     => ex.AsAccessDeniedError(),
					NotAuthenticatedException => ex.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
				}
			);
		}
	}

	public async ValueTask<Result<UserDetails, GetUserError>> GetUser(string loginName, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrEmpty(loginName);

		try {
			return await ListAllCore(loginName, cancellationToken)
				.FirstAsync(cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		} catch (Exception ex) {
			return Result.Failure<UserDetails, GetUserError>(
				ex switch {
					AccessDeniedException => ex.AsAccessDeniedError(),
					_                     => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
				}
			);
		}
	}

	public IAsyncEnumerable<UserDetails> ListAllAsync(CancellationToken cancellationToken = default) => ListAllCore(null, cancellationToken);

	IAsyncEnumerable<UserDetails> ListAllCore(string? loginName, CancellationToken cancellationToken = default) {
		var req = loginName is not null
			? new DetailsReq { Options = { LoginName = loginName } }
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
		ArgumentException.ThrowIfNullOrEmpty(loginName);
		ArgumentException.ThrowIfNullOrEmpty(newPassword);

		try {
			await ServiceClient.ResetPasswordAsync(
				new ResetPasswordReq {
					Options = new ResetPasswordReq.Types.Options {
						NewPassword = newPassword,
						LoginName   = loginName
					}
				},
				cancellationToken: cancellationToken
			);

			return new Result<Success, ResetPasswordError>();
		} catch (Exception ex) {
			return Result.Failure<Success, ResetPasswordError>(
				ex switch {
					AccessDeniedException     => ex.AsAccessDeniedError(),
					NotAuthenticatedException => ex.AsNotAuthenticatedError(),
					_                         => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
				}
			);
		}
	}
}
