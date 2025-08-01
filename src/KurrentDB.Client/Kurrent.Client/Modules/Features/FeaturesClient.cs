using EventStore.Client;
using Kurrent.Client.Features;
using static EventStore.Client.ServerFeatures.ServerFeatures;

namespace Kurrent.Client;

[PublicAPI]
public class FeaturesClient {
	internal FeaturesClient(KurrentClient source) =>
		ServiceClient = new ServerFeaturesClient(source.LegacyCallInvoker);

	ServerFeaturesClient ServiceClient { get; }

	/// <summary>
	/// Gets server information including features and their enablement status.
	/// </summary>
	/// <param name="cancellationToken">
	/// Cancellation token to cancel the request if needed.
	/// </param>
	/// <returns>Server information with features.</returns>
	public async Task<ServerFeatures> GetFeatures(CancellationToken cancellationToken = default) {
		// Get the raw methods and their features from the server
		var response = await ServiceClient
			.GetSupportedMethodsAsync(CustomEmptyRequest, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		// In the future, we can expand this to include other server information
		return new ServerFeatures {
			Version  = response.EventStoreServerVersion
		};
	}

	// Custom empty request to avoid using the default one from the library.... sigh... -_-'
	static readonly Empty CustomEmptyRequest = new();
}
