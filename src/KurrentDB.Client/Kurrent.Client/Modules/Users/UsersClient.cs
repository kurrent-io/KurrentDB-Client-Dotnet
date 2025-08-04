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

    public async ValueTask<Result<Success, ChangeUserPasswordError>> ChangeUserPassword(
        string loginName, string currentPassword, string newPassword, CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ChangePasswordReq {
                Options = new() {
                    CurrentPassword = currentPassword,
                    NewPassword     = newPassword,
                    LoginName       = loginName
                }
            };

            // TODO SS: does the user exist? do we want to be explicit about this?

            await ServiceClient
                .ChangePasswordAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, ChangeUserPasswordError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(ChangeUserPassword), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(ChangeUserPassword), ex);
        }
    }

    public async ValueTask<Result<Success, CreateUserError>> CreateUser(
        string loginName, string fullName, string[] groups, string password,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new CreateReq {
                Options = new() {
                    LoginName = loginName,
                    FullName  = fullName,
                    Password  = password,
                    Groups    = { groups }
                }
            };

            // TODO SS: does the user already exist? do we want to be explicit about this?

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, CreateUserError>(
                ex.StatusCode switch {
                    StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(CreateUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(CreateUser), ex);
        }
    }

    public async ValueTask<Result<Success, DeleteUserError>> DeleteUser(string loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DeleteReq { Options = new() { LoginName = loginName } };

            // TODO SS: does the user exist? do we want to be explicit about this?

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, DeleteUserError>(
                ex.StatusCode switch {
                    StatusCode.NotFound         => ex.AsUserNotFoundError(),
                    StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(DeleteUser), ex);
        }
    }

    public async ValueTask<Result<Success, DisableUserError>> DisableUser(string loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DisableReq { Options = new() { LoginName = loginName } };

            // TODO SS: does the user exist? do we want to be explicit about this?

            await ServiceClient
                .DisableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, DisableUserError>(
                ex.StatusCode switch {
                    StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(DisableUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(DisableUser), ex);
        }
    }

    public async ValueTask<Result<Success, EnableUserError>> EnableUser(string loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new EnableReq { Options = new() { LoginName = loginName } };

            // TODO SS: does the user exist? do we want to be explicit about this?

            await ServiceClient
                .EnableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, EnableUserError>(
                ex.StatusCode switch {
                    StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(EnableUser), ex);
        }
    }

    public async ValueTask<Result<UserDetails, GetUserError>> GetUser(string loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DetailsReq { Options = new() { LoginName = loginName } };

            using var call = ServiceClient.Details(request, cancellationToken: cancellationToken);

            var user = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(static resp => new UserDetails {
                    LoginName       = resp.UserDetails.LoginName,
                    FullName        = resp.UserDetails.FullName,
                    Groups          = resp.UserDetails.Groups.ToArray(),
                    Disabled        = resp.UserDetails.Disabled,
                    DateLastUpdated = resp.UserDetails.LastUpdated?.TicksSinceEpoch.FromTicksSinceEpoch()
                })
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            return user ?? Result.Failure<UserDetails, GetUserError>(new ErrorDetails.UserNotFound(metadata => metadata.With("logingName", loginName)));
        }
        catch (RpcException ex) {
            return Result.Failure<UserDetails, GetUserError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(GetUser), ex);
        }
    }

    public async ValueTask<Result<List<UserDetails>, ListAllUsersError>> ListAllUsers(CancellationToken cancellationToken = default) {
        try {
            using var call = ServiceClient.Details(new DetailsReq(), cancellationToken: cancellationToken);

            var users = await call.ResponseStream
                .ReadAllAsync(cancellationToken)
                .Select(static resp => new UserDetails { // TODO SS: Create a mapper for mapping user details
                    LoginName       = resp.UserDetails.LoginName,
                    FullName        = resp.UserDetails.FullName,
                    Groups          = resp.UserDetails.Groups.ToArray(),
                    Disabled        = resp.UserDetails.Disabled,
                    DateLastUpdated = resp.UserDetails.LastUpdated?.TicksSinceEpoch.FromTicksSinceEpoch()
                })
                .ToListAsync(cancellationToken: cancellationToken);

            return users;
        }
        catch (RpcException ex) {
            return Result.Failure<List<UserDetails>, ListAllUsersError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(GetUser), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(ListAllUsers), ex);
        }
    }

    public async ValueTask<Result<Success, ResetUserPasswordError>> ResetUserPassword(
        string loginName, string newPassword, CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ResetPasswordReq {
                Options = new() {
                    NewPassword = newPassword,
                    LoginName   = loginName
                }
            };

            // TODO SS: does the user exist? do we want to be explicit about this?

            await ServiceClient
                .ResetPasswordAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, ResetUserPasswordError>(
                ex.StatusCode switch {
                    StatusCode.Unauthenticated  => ex.AsNotAuthenticatedError(),
                    StatusCode.PermissionDenied => ex.AsAccessDeniedError(),
                    _                           => throw KurrentClientException.CreateUnknown(nameof(ResetUserPassword), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentClientException.CreateUnknown(nameof(ResetUserPassword), ex);
        }
    }
}
