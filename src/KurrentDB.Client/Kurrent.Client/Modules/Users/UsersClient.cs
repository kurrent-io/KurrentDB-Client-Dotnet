// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Grpc;
using KurrentDB.Client;
using KurrentDB.Protocol.Users.V1;
using AsyncStreamReaderExtensions = Kurrent.Grpc.AsyncStreamReaderExtensions;
using UsersServiceClient = KurrentDB.Protocol.Users.V1.Users.UsersClient;

namespace Kurrent.Client.Users;

public class UsersClient : ClientModuleBase {
    internal UsersClient(KurrentClient client) : base(client) =>
        ServiceClient = new(client.LegacyCallInvoker);

    UsersServiceClient ServiceClient { get; }

    public async ValueTask<Result<Success, CreateUserError>> Create(
        LoginName loginName, string fullName, string[] groups, string password,
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

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, CreateUserError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists    => new ErrorDetails.AlreadyExists(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, DeleteUserError>> Delete(LoginName loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DeleteReq { Options = new() { LoginName = loginName } };

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteUserError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, DisableUserError>> Disable(LoginName loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DisableReq { Options = new() { LoginName = loginName } };

            await ServiceClient
                .DisableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DisableUserError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, EnableUserError>> Enable(LoginName loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new EnableReq { Options = new() { LoginName = loginName } };

            await ServiceClient
                .EnableAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, EnableUserError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, ChangeUserPasswordError>> ChangePassword(
        LoginName loginName, string currentPassword, string newPassword, CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ChangePasswordReq {
                Options = new() {
                    CurrentPassword = currentPassword,
                    NewPassword     = newPassword,
                    LoginName       = loginName
                }
            };

            await ServiceClient
                .ChangePasswordAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ChangeUserPasswordError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<Success, ResetUserPasswordError>> ResetPassword(
        LoginName loginName, string newPassword, CancellationToken cancellationToken = default
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

            return Results.Success;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, ResetUserPasswordError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<UserDetails, GetUserError>> GetDetails(LoginName loginName, CancellationToken cancellationToken = default) {
        try {
            var request = new DetailsReq { Options = new() { LoginName = loginName } };

            using var call = ServiceClient.Details(request, cancellationToken: cancellationToken);

            var user = await AsyncStreamReaderExtensions.ReadAllAsync(call.ResponseStream, cancellationToken)
                .Select(static resp => new UserDetails {
                    LoginName       = resp.UserDetails.LoginName,
                    FullName        = resp.UserDetails.FullName,
                    Groups          = resp.UserDetails.Groups.ToArray(),
                    Disabled        = resp.UserDetails.Disabled,
                    DateLastUpdated = resp.UserDetails.LastUpdated?.TicksSinceEpoch.FromTicksSinceEpoch()
                })
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            return user ?? Result.Failure<UserDetails, GetUserError>(new ErrorDetails.NotFound());
        }
        catch (RpcException rex) {
            return Result.Failure<UserDetails, GetUserError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public async ValueTask<Result<List<UserDetails>, ListAllUsersError>> List(CancellationToken cancellationToken = default) {
        try {
            using var call = ServiceClient.Details(new DetailsReq(), cancellationToken: cancellationToken);

            var users = await AsyncStreamReaderExtensions.ReadAllAsync(call.ResponseStream, cancellationToken)
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
        catch (RpcException rex) {
            return Result.Failure<List<UserDetails>, ListAllUsersError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }
}
