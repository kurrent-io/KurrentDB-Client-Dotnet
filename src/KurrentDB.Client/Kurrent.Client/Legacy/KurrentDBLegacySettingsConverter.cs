using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using KurrentDB.Client;
using UserCredentials = KurrentDB.Client.UserCredentials;

namespace Kurrent.Client.Legacy;

public static class KurrentDBLegacySettingsConverter {
    /// <summary>
    /// Converts the new options format to the legacy client settings.
    /// </summary>
    /// <param name="options">The KurrentClientOptions to convert</param>
    /// <returns>Equivalent KurrentDBClientSettings configured from the options</returns>
    /// <remarks>
    /// <para>
    /// This method enables backward compatibility with code expecting the legacy settings format
    /// while allowing users to benefit from the more structured options builder pattern.
    /// </para>
    /// </remarks>
    public static KurrentDBClientSettings ConvertToLegacySettings(this KurrentClientOptions options) {
        var connectivitySettings = new KurrentDBClientConnectivitySettings {
            // Gossip configuration
            GossipSeeds         = options.ConnectionScheme == KurrentConnectionScheme.Discover ? options.Endpoints : [],
            MaxDiscoverAttempts = options.Gossip.MaxDiscoverAttempts,
            DiscoveryInterval   = options.Gossip.DiscoveryInterval,
            GossipTimeout       = options.Gossip.Timeout,
            NodePreference      = options.Gossip.ReadPreference,

            // Resilience-related connectivity settings
            KeepAliveInterval = options.Resilience.KeepAliveInterval,
            KeepAliveTimeout  = options.Resilience.KeepAliveTimeout,
        };

        options.Security.Transport.Switch(
            noTransportSecurity => connectivitySettings.Insecure = true,
            standardTls         => connectivitySettings.TlsVerifyCert = standardTls.VerifyServerCertificate,
            fileCertificate     => {
                connectivitySettings.TlsVerifyCert = fileCertificate.VerifyServerCertificate;

                var caPath = Path.GetFullPath(options.Security.Transport.AsFileCertificateTls.CaPath);
                if (!string.IsNullOrEmpty(caPath) && !File.Exists(caPath))
                    throw new CryptographicException("Failed to load certificate. File was not found.");

                try {
                    #if NET9_0_OR_GREATER
                    connectivitySettings.TlsCaFile = X509CertificateLoader.LoadCertificateFromFile(caPath);
                    #else
                    connectivitySettings.TlsCaFile = new X509Certificate2(caPath);
                    #endif
                }
                catch (CryptographicException) {
                    throw new CryptographicException("Failed to load certificate. Invalid file format.");
                }
            },
            x509Certificate => {
                connectivitySettings.TlsVerifyCert = x509Certificate.VerifyServerCertificate;
                connectivitySettings.TlsCaFile     = x509Certificate.Certificate;
            }
        );

        // Handle client certificate
        if (options.Security.Authentication.Value is X509CertificateCredentials x509Credentials)
            connectivitySettings.ClientCertificate = x509Credentials.Certificate;

        if (options.ConnectionScheme == KurrentConnectionScheme.Direct)
            connectivitySettings.Address = options.Address;

        // Convert retry settings
        var retrySettings = new KurrentDBClientRetrySettings {
            IsEnabled            = options.Resilience.Retry.Enabled,
            MaxAttempts          = options.Resilience.Retry.MaxAttempts,
            InitialBackoff       = options.Resilience.Retry.InitialBackoff,
            MaxBackoff           = options.Resilience.Retry.MaxBackoff,
            BackoffMultiplier    = options.Resilience.Retry.BackoffMultiplier,
            RetryableStatusCodes = options.Resilience.Retry.RetryableStatusCodes.ToArray()
        };

        // Convert default credentials
        var defaultCredentials = options.Security.Authentication.Value is UserCredentials credentials
            ? new UserCredentials(credentials.Username ?? string.Empty, credentials.Password ?? string.Empty)
            : UserCredentials.Empty;

        return new KurrentDBClientSettings {
            ConnectionName       = options.ConnectionName,
            DefaultDeadline      = options.Resilience.Deadline,
            DefaultCredentials   = defaultCredentials,
            ConnectivitySettings = connectivitySettings,
            RetrySettings        = retrySettings,
            Interceptors         = options.Interceptors.ToArray(),
            LoggerFactory        = options.LoggerFactory,
            OperationOptions     = KurrentDBClientOperationOptions.Default,
        };
    }
}
