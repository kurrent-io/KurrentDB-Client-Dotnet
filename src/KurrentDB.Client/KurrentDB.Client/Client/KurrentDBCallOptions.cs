using Grpc.Core;
using static System.Threading.Timeout;

namespace KurrentDB.Client;

static class KurrentDBCallOptions {
	// deadline falls back to infinity
	public static CallOptions CreateStreaming(
		KurrentDBClientSettings settings, TimeSpan? deadline = null, UserCredentials? userCredentials = null, CancellationToken cancellationToken = default) =>
		Create(settings, deadline, userCredentials, cancellationToken);

	// deadline falls back to connection DefaultDeadline
	public static CallOptions CreateNonStreaming(KurrentDBClientSettings settings, CancellationToken cancellationToken) =>
		Create(settings, settings.DefaultDeadline, settings.DefaultCredentials, cancellationToken);

	public static CallOptions CreateNonStreaming(
		KurrentDBClientSettings settings, TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken) =>
		Create(settings, deadline ?? settings.DefaultDeadline, userCredentials, cancellationToken);

	static CallOptions Create(KurrentDBClientSettings settings, TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken) {
		return new(
			cancellationToken: cancellationToken,
			deadline: DeadlineAfter(deadline),
			// could this be because of the way the client is created?
			// and the core client cannot have this header added to every call?
			// ensure the headers interceptor works, if not let this value be added here...
			// headers: new() {
			// 	{
			// 		Constants.Headers.RequiresLeader,
			// 		settings.ConnectivitySettings.NodePreference == NodePreference.Leader
			// 			? bool.TrueString
			// 			: bool.FalseString
			// 	}
			// },
			credentials: (userCredentials ?? settings.DefaultCredentials) is not null
				? CallCredentials.FromInterceptor(async (_, metadata) => {
					var credentials = userCredentials ?? settings.DefaultCredentials;

					var authorizationHeader = await settings.OperationOptions
						.GetAuthenticationHeaderValue(credentials!, CancellationToken.None)
						.ConfigureAwait(false);

					metadata.Add(Constants.Headers.Authorization, authorizationHeader);
				})
				: null
		);

		static DateTime? DeadlineAfter(TimeSpan? timeoutAfter) =>
			!timeoutAfter.HasValue
				? null
				: timeoutAfter.Value == TimeSpan.MaxValue || timeoutAfter.Value == InfiniteTimeSpan
					? DateTime.MaxValue
					: DateTime.UtcNow.Add(timeoutAfter.Value);
	}
}
