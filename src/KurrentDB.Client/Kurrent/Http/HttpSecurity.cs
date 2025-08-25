namespace Kurrent.Http;

static class HttpSecurity {
    /// <summary>
    /// Enables HTTP/2 over unencrypted connections.
    /// </summary>
    /// <remarks>
    /// By default, .NET requires TLS for HTTP/2 connections to align with browser security standards.
    /// However, in many scenarios services communicate over plain HTTP/2 without TLS encryption.
    /// <para/>
    /// This switch is necessary when:
    /// <para/>- Connecting to development/test services that don't have TLS certificates configured
    /// <para/>- Communicating with internal microservices behind a secure network perimeter
    /// <para/>- Working with gRPC services that use plain HTTP/2 (common in non-.NET ecosystems)
    /// <para/>- Developing on macOS where ASP.NET Core's Kestrel doesn't support HTTP/2 with TLS (pre-.NET 8)
    /// <para/>
    /// Without this switch, attempting to use HTTP/2 over plain HTTP will result in:
    /// <para/>- HttpRequestException: "The SSL connection could not be established"
    /// <para/>- Or the client falling back to HTTP/1.1, which may not be supported by the server
    /// <para/>
    /// IMPORTANT: This switch MUST be set before creating the HttpClient/HttpMessageHandler.
    /// Setting it afterward has no effect on already-created handlers.
    /// <para/>
    /// SECURITY NOTE: Only use unencrypted HTTP/2 in trusted environments. Production services
    /// exposed to the internet should always use TLS encryption.
    /// </remarks>
    public static void EnableHttp2UnencryptedSupport() =>
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

    // public static RemoteCertificateValidator CreateRemoteCertificateValidator(X509Certificate2 caCertificate, ILogger logger) =>
    //     new(caCertificate, logger);
    //
    // /// <summary>
    // /// Validates the server's certificate against a custom CA certificate on SSL/TLS connections.
    // /// </summary>
    // /// <param name="CaCertificate">
    // /// The custom CA certificate used to validate the server's certificate.
    // /// This should be a valid X509Certificate2 object representing the CA that signed the server's certificate.
    // /// </param>
    // /// <param name="Logger">
    // /// The logger instance used to log errors and information during the validation process.
    // /// </param>
    // public record RemoteCertificateValidator(X509Certificate2 CaCertificate, ILogger Logger) {
    //     bool Callback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors) {
    //         if (errors != SslPolicyErrors.None)
    //             Logger.LogError("SSL Policy Errors: {SslPolicyErrors}", errors);
    //
    //         if (certificate is null || chain is null) {
    //             Logger.LogError("Certificate or chain is null. Ensure the certificate is properly loaded.");
    //             return false;
    //         }
    //
    //         // not sure if I need this check, so test test test...
    //         if (certificate is not X509Certificate2 peerCertificate) {
    //             try {
    //                 peerCertificate = new X509Certificate2(certificate);
    //             }
    //             catch (Exception ex) {
    //                 Logger.LogError(
    //                     ex,
    //                     "Failed to convert certificate to X509Certificate2 format. " +
    //                     "Ensure the certificate is in a valid format (e.g., PEM, CRT, CER)."
    //                 );
    //
    //                 return false;
    //             }
    //         }
    //
    //         chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
    //         chain.ChainPolicy.CustomTrustStore.Add(CaCertificate);
    //
    //         var chainIsValid = chain.Build(peerCertificate);
    //
    //         // not sure if I need this check, so test test test...
    //         if (!chainIsValid)
    //             Logger.LogError("X.509 Chain Status: {ChainStatusInformation}", chain.ChainStatus);
    //
    //         return chainIsValid;
    //     }
    //
    //     public static implicit operator RemoteCertificateValidationCallback(RemoteCertificateValidator _) => _.Callback;
    // }
}
