using System.Net;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using KurrentDB.Client;

namespace Kurrent.Client.Legacy;

static class ChannelFactoryExtensions {
    const int MaxReceiveMessageLength = 17 * 1024 * 1024; // 17MB

    public static GrpcChannel CreateChannel(this KurrentDBClientSettings settings, DnsEndPoint endPoint, out GrpcChannelOptions options) {
        var address = new UriBuilder {
            Scheme = settings.ConnectivitySettings.Insecure ? Uri.UriSchemeHttp : Uri.UriSchemeHttps,
            Host   = endPoint.Host,
            Port   = endPoint.Port
        }.Uri;

        var httpClient = settings.CreateHttpClient(address);

        options = new GrpcChannelOptions {
            ServiceConfig = settings.RetrySettings.IsEnabled
                ? new() { MethodConfigs = { settings.RetrySettings.GetRetryMethodConfig() } }
                : null,

            HttpClient            = httpClient,
            LoggerFactory         = settings.LoggerFactory,
            Credentials           = settings.ChannelCredentials,
            DisposeHttpClient     = true,
            MaxReceiveMessageSize = MaxReceiveMessageLength
        };

        return GrpcChannel.ForAddress(address, options);
    }

    public static HttpClient CreateHttpClient(this KurrentDBClientSettings settings, Uri address) {
        if (settings.ConnectivitySettings.Insecure) {
            //this must be switched on before creation of the HttpMessageHandler
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        return new(CreateHandler(settings), disposeHandler: true) {
            BaseAddress           = address,
            Timeout               = Timeout.InfiniteTimeSpan,
            DefaultRequestVersion = new Version(2, 0)
        };

        static HttpMessageHandler CreateHandler(KurrentDBClientSettings settings) {
            if (settings.CreateHttpMessageHandler is not null)
                return settings.CreateHttpMessageHandler.Invoke();

            var handler = new SocketsHttpHandler {
                KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
                KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
                EnableMultipleHttp2Connections = true
            };

            if (settings.ConnectivitySettings.Insecure) {
                handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                return handler;
            }

            if (settings.ConnectivitySettings.ClientCertificate is not null) {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection {
                    settings.ConnectivitySettings.ClientCertificate
                };
            }

            handler.SslOptions.RemoteCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
                false => delegate { return true; },
                true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
                    if (certificate is not X509Certificate2 peerCertificate || chain is null) return false;

                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);
                    return chain.Build(peerCertificate);
                },
                _ => null
            };

            return handler;
        }
    }
}
