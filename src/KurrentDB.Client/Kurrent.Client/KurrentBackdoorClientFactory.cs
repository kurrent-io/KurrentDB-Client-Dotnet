using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Kurrent.Client.Legacy;
using Kurrent.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;

namespace Kurrent.Client;

class KurrentBackdoorClientFactory : IDisposable {
    public KurrentBackdoorClientFactory(KurrentDBLegacyCallInvoker invoker, KurrentClientOptions options) {
        Invoker = invoker;
        Logger  = options.LoggerFactory.CreateLogger("KurrentBackdoorClient");

        if (options.Security.Transport.IsInsecure)
            HttpSecurity.EnableHttp2UnencryptedSupport();

        ParseTargetNodeUri = CreateTargetNodeUriParser(options.HttpUriScheme);

        BackDoorHandler = new ResilienceHandler(CreateResiliencePipeline(options.Resilience)) {
            InnerHandler = new EnsureSuccessStatusHandler {
                InnerHandler = CreatePrimaryHandler(options.Security, options.Resilience, Logger)
            }
        };
    }

    KurrentDBLegacyCallInvoker Invoker            { get; }
    ILogger                    Logger             { get; }
    HttpMessageHandler         BackDoorHandler    { get; }
    Func<string, Uri>          ParseTargetNodeUri { get; }

    public HttpClient GetClient(Uri address) {
        return new HttpClient(BackDoorHandler, disposeHandler: false) {
            BaseAddress           = address,
            Timeout               = Timeout.InfiniteTimeSpan,
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrHigher
        };
    }

    public HttpClient GetClient(string targetNode) => GetClient(ParseTargetNodeUri(targetNode));

    public HttpClient GetClient() => GetClient(Invoker.ChannelTarget);

    public void Dispose() => BackDoorHandler.Dispose();

    static Func<string, Uri> CreateTargetNodeUriParser(string httpUriScheme) =>
        target => new Uri($"{httpUriScheme}://{target}");

    static ResiliencePipeline<HttpResponseMessage> CreateResiliencePipeline(KurrentClientResilienceOptions options) {
        var httpResilienceOptions = new HttpStandardResilienceOptions {
            Retry = {
                // ShouldHandle = new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>(ex =>
                //     ex.StatusCode is HttpStatusCode.RequestTimeout
                //                   or HttpStatusCode.GatewayTimeout
                //                   or HttpStatusCode.ServiceUnavailable
                // ),
                MaxRetryAttempts = options.Retry.MaxAttempts,
                Delay            = options.Retry.InitialBackoff,
                MaxDelay         = options.Retry.MaxBackoff,
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true
            }
        };

        if (options.Deadline is not null)
            httpResilienceOptions.TotalRequestTimeout.Timeout = options.Deadline.Value;

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRateLimiter(httpResilienceOptions.RateLimiter)
            .AddTimeout(httpResilienceOptions.TotalRequestTimeout)
            .AddRetry(httpResilienceOptions.Retry)
            .AddCircuitBreaker(httpResilienceOptions.CircuitBreaker)
            .AddTimeout(httpResilienceOptions.AttemptTimeout)
            .Build();
    }

    static SocketsHttpHandler CreatePrimaryHandler(KurrentClientSecurityOptions security, KurrentClientResilienceOptions resilience, ILogger logger) {
        var handler = new SocketsHttpHandler {
            KeepAlivePingDelay             = resilience.KeepAliveInterval,
            KeepAlivePingTimeout           = resilience.KeepAliveTimeout,
            EnableMultipleHttp2Connections = true
        };

        if (security.Transport.IsInsecure) {
            handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
            return handler;
        }

        if (security.Authentication.TryGetCertificate(out var clientCertificate))
            handler.SslOptions.ClientCertificates = new() { clientCertificate };

        if (security.Transport.VerifyServerCertificate && security.Transport.TryGetCertificate(out var caCertificate))
            handler.SslOptions.RemoteCertificateValidationCallback = new RemoteCertificateValidator(caCertificate, logger);

        return handler;
    }

    /// <summary>
    /// Validates the server's certificate against a custom CA certificate on SSL/TLS connections.
    /// </summary>
    /// <param name="CaCertificate">
    /// The custom CA certificate used to validate the server's certificate.
    /// This should be a valid X509Certificate2 object representing the CA that signed the server's certificate.
    /// </param>
    /// <param name="Logger">
    /// The logger instance used to log errors and information during the validation process.
    /// </param>
    record RemoteCertificateValidator(X509Certificate2 CaCertificate, ILogger Logger) {
        bool Callback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors) {
            if (errors != SslPolicyErrors.None)
                Logger.LogError("SSL Policy Errors: {SslPolicyErrors}", errors);

            if (certificate is null || chain is null) {
                Logger.LogError("Certificate or chain is null. Ensure the certificate is properly loaded.");
                return false;
            }

            // not sure if I need this check, so test test test...
            if (certificate is not X509Certificate2 peerCertificate) {
                try {
                    peerCertificate = new X509Certificate2(certificate);
                }
                catch (Exception ex) {
                    Logger.LogError(
                        ex,
                        "Failed to convert certificate to X509Certificate2 format. " +
                        "Ensure the certificate is in a valid format (e.g., PEM, CRT, CER)."
                    );

                    return false;
                }
            }

            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(CaCertificate);

            var chainIsValid = chain.Build(peerCertificate);

            // not sure if I need this check, so test test test...
            if (!chainIsValid)
                Logger.LogError("X.509 Chain Status: {ChainStatusInformation}", chain.ChainStatus);

            return chainIsValid;
        }

        public static implicit operator RemoteCertificateValidationCallback(RemoteCertificateValidator _) => _.Callback;
    }
}
