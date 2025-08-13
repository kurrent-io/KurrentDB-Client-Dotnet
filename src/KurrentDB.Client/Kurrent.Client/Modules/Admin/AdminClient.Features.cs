using EventStore.Client;
using Kurrent.Client.Features;

namespace Kurrent.Client.Admin;

public partial class AdminClient {
	/// <summary>
	/// Gets server information including features and their enablement status.
	/// </summary>
	/// <param name="cancellationToken">
	/// Cancellation token to cancel the request if needed.
	/// </param>
	/// <returns>Server information with features.</returns>
	public async Task<ServerFeatures> GetFeatures(CancellationToken cancellationToken = default) {
		// Get the raw methods and their features from the server
		var response = await FeaturesServiceClient
			.GetSupportedMethodsAsync(EmptyRequest, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		// In the future, we can expand this to include other server information
		return new ServerFeatures {
			Version  = response.EventStoreServerVersion
		};
	}
}
