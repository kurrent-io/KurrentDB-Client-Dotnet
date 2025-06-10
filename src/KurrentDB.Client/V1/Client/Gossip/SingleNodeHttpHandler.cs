namespace KurrentDB.Client;

class SingleNodeHttpHandler(KurrentDBClientSettings settings) : DelegatingHandler {
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
		request.RequestUri = new UriBuilder(request.RequestUri!) {
			Scheme = settings.ConnectivitySettings.Address?.Scheme ?? (settings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps)
		}.Uri;
		return base.SendAsync(request, cancellationToken);
	}
}
