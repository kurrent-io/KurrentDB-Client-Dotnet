using KurrentDb.Client;
using Polly;
using static System.TimeSpan;

namespace KurrentDb.Client.Tests;

public static class KurrentDbClientExtensions {
	public static Task CreateUserWithRetry(
		this KurrentDbUserManagementClient client, string loginName, string fullName, string[] groups, string password,
		UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
	) =>
		Policy.Handle<NotAuthenticatedException>()
			.WaitAndRetryAsync(200, _ => FromMilliseconds(100))
			.ExecuteAsync(
				ct => client.CreateUserAsync(
					loginName,
					fullName,
					groups,
					password,
					userCredentials: userCredentials,
					cancellationToken: ct
				),
				cancellationToken
			);
}
