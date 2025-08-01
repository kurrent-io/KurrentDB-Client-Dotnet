using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace KurrentDB.Client;

class HttpFallback : IDisposable {
    readonly string                _addressScheme;
    readonly UserCredentials?      _defaultCredentials;
    readonly HttpClient            _httpClient;
    readonly JsonSerializerOptions _jsonSettings;

    internal HttpFallback(KurrentDBClientSettings settings) {
        _addressScheme = settings.ConnectivitySettings.Address?.Scheme ?? (settings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps);
        _defaultCredentials = settings.DefaultCredentials;

        var handler = new HttpClientHandler();
        if (!settings.ConnectivitySettings.Insecure) {
            handler.ClientCertificateOptions = ClientCertificateOption.Manual;

            if (settings.ConnectivitySettings.ClientCertificate is not null)
                handler.ClientCertificates.Add(settings.ConnectivitySettings.ClientCertificate);

            handler.ServerCertificateCustomValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
                false => delegate { return true; },
                true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
                    if (certificate is null || chain is null) return false;

                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.TrustMode      = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);

                    return chain.Build(certificate);
                },
                _ => null
            };
        }

        _httpClient = new HttpClient(handler);
        if (settings.DefaultDeadline.HasValue) _httpClient.Timeout = settings.DefaultDeadline.Value;

        _jsonSettings = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void Dispose() => _httpClient.Dispose();

    internal async Task<T> HttpGetAsync<T>(
        string path, ChannelInfo channelInfo, TimeSpan? deadline, UserCredentials? userCredentials,
        Action onNotFound, CancellationToken cancellationToken
    ) {
        var request = CreateRequest(
            path, HttpMethod.Get, channelInfo,
            userCredentials
        );

        var httpResult = await HttpSendAsync(
            request, onNotFound, deadline,
            cancellationToken
        ).ConfigureAwait(false);

        var json = await httpResult.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        var result = JsonSerializer.Deserialize<T>(json, _jsonSettings);
        if (result == null)
            throw new InvalidOperationException("Unable to deserialize response into object of type " + typeof(T));

        return result;
    }

    internal async Task HttpPostAsync(
        string path, string query, ChannelInfo channelInfo, TimeSpan? deadline,
        UserCredentials? userCredentials, Action onNotFound, CancellationToken cancellationToken
    ) {
        var request = CreateRequest(
            path, query, HttpMethod.Post,
            channelInfo, userCredentials
        );

        await HttpSendAsync(
            request, onNotFound, deadline,
            cancellationToken
        ).ConfigureAwait(false);
    }

    async Task<HttpResponseMessage> HttpSendAsync(HttpRequestMessage request, Action onNotFound, TimeSpan? deadline, CancellationToken cancellationToken) {
        if (!deadline.HasValue)
            return await HttpSendAsync(request, onNotFound, cancellationToken).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(deadline.Value);

        return await HttpSendAsync(request, onNotFound, cts.Token).ConfigureAwait(false);
    }

    async Task<HttpResponseMessage> HttpSendAsync(HttpRequestMessage request, Action onNotFound, CancellationToken cancellationToken) {
        var httpResult = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (httpResult.IsSuccessStatusCode)
            return httpResult;

        if (httpResult.StatusCode == HttpStatusCode.Unauthorized)
            throw new AccessDeniedException();

        if (httpResult.StatusCode == HttpStatusCode.NotFound) onNotFound();

        throw new Exception($"The HTTP request failed with status code: {httpResult.StatusCode}");
    }

    HttpRequestMessage CreateRequest(string path, HttpMethod method, ChannelInfo channelInfo, UserCredentials? credentials) =>
        CreateRequest(
            path, "", method,
            channelInfo, credentials
        );

    HttpRequestMessage CreateRequest(
        string path, string query, HttpMethod method, ChannelInfo channelInfo,
        UserCredentials? credentials
    ) {
        var uriBuilder = new UriBuilder($"{_addressScheme}://{channelInfo.Channel.Target}") {
            Path  = path,
            Query = query
        };

        var httpRequest = new HttpRequestMessage(method, uriBuilder.Uri);
        httpRequest.Headers.Add("accept", "application/json");

        credentials ??= _defaultCredentials;

        if (credentials != null)
            httpRequest.Headers.Add(Constants.Headers.Authorization, credentials.ToString());

        return httpRequest;
    }
}
