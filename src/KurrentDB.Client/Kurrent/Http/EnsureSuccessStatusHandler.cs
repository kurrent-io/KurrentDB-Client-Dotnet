namespace Kurrent.Http;

class EnsureSuccessStatusHandler : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
