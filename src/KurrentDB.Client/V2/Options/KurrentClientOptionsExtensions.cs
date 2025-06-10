using System.Text;
using Kurrent.Client.Security;
using KurrentDB.Client;

namespace Kurrent.Client;

#pragma warning disable CS8524
public static class KurrentClientOptionsExtensions {
    /// <summary>
    /// Converts the current options to a valid KurrentDB connection string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generates a connection string that represents these client options, matching the format expected by KurrentDB.
    /// For X509Certificate2, we can't extract file paths, so we skip these parameters bu add instead `userCert=:from_memory:` or `tlsCa=:from_memory:`.
    /// </para>
    /// </remarks>
    /// <returns>A connection string that can be used to connect to KurrentDB.</returns>
    public static string GenerateConnectionString(this KurrentClientOptions options) {
        var builder = new StringBuilder();

        // Add scheme
        builder.Append(options.ConnectionUriScheme);
        builder.Append("://");

        // Add authentication if present
        if (options.Security.Authentication.Value is BasicCredentials credentials) {
            builder.Append(Uri.EscapeDataString(credentials.Username));

            if (!string.IsNullOrEmpty(credentials.Password)) {
                builder.Append(':');
                builder.Append(Uri.EscapeDataString(credentials.Password));
            }

            builder.Append('@');
        }

        // if (options.Security.Authentication.Value is BasicUserCredentials credentials) {
        //     var credentialsBuilder = new StringBuilder();
        //     credentialsBuilder.Append(credentials.Username);
        //
        //     if (!string.IsNullOrEmpty(credentials.Password)) {
        //         credentialsBuilder.Append(':');
        //         credentialsBuilder.Append(credentials.Password);
        //     }
        //
        //     var final = Uri.EscapeDataString(credentialsBuilder.ToString());
        //     builder.Append(final);
        //     builder.Append('@');
        // }

        // Add endpoints
        if (options.Endpoints.Length > 0) {
            var endpoints = string.Join(",", options.Endpoints.Select(ep => $"{ep.Host}:{ep.Port}"));
            builder.Append(endpoints);
        }

        // Add query parameters
        var parameters = new List<string>();

        // Transport security parameters
        options.Security.Transport.Switch(
            _   => parameters.Add("tls=false"),
            tls  => {
                if (!tls.VerifyServerCertificate) parameters.Add("tlsVerifyCert=false");
            },
            file => {
                if (!file.VerifyServerCertificate) parameters.Add("tlsVerifyCert=false");
                parameters.Add($"tlsCaFile={Uri.EscapeDataString(file.CaPath)}");
            },
            x509 => {
                if (!x509.VerifyServerCertificate) parameters.Add("tlsVerifyCert=false");
                parameters.Add("tlsCa=:from_memory:");
            }
        );

        // Certificate authentication parameters
        switch (options.Security.Authentication.Value) {
            case FileCertificateCredentials certFileCredentials:
                parameters.Add($"userCertFile={Uri.EscapeDataString(certFileCredentials.CertificatePath)}");
                parameters.Add($"userKeyFile={Uri.EscapeDataString(certFileCredentials.KeyPath)}");
                break;

            case X509CertificateCredentials:
                parameters.Add("userCert=:from_memory:");
                break;
        }

        // Connection name parameter (only if not default)
        if (!string.IsNullOrEmpty(options.ConnectionName) && !options.ConnectionName.StartsWith("conn-"))
            parameters.Add($"connectionName={Uri.EscapeDataString(options.ConnectionName)}");

        // Gossip parameters
        if (options.Gossip.ReadPreference != NodePreference.Random)
            parameters.Add($"nodePreference={options.Gossip.ReadPreference.ToString().ToLowerInvariant()}");

        if (options.Gossip.MaxDiscoverAttempts != KurrentClientGossipOptions.Default.MaxDiscoverAttempts)
            parameters.Add($"maxDiscoverAttempts={options.Gossip.MaxDiscoverAttempts}");

        if (options.Gossip.DiscoveryInterval != KurrentClientGossipOptions.Default.DiscoveryInterval)
            parameters.Add($"discoveryInterval={(int)options.Gossip.DiscoveryInterval.TotalMilliseconds}");

        if (options.Gossip.Timeout != KurrentClientGossipOptions.Default.Timeout)
            parameters.Add($"gossipTimeout={(int)options.Gossip.Timeout.TotalSeconds}");

        // Resilience parameters
        if (options.Resilience.KeepAliveInterval != KurrentClientResilienceOptions.Default.KeepAliveInterval &&
            options.Resilience.KeepAliveInterval != Timeout.InfiniteTimeSpan)
            parameters.Add($"keepAliveInterval={(int)options.Resilience.KeepAliveInterval.TotalSeconds}");

        if (options.Resilience.KeepAliveTimeout != KurrentClientResilienceOptions.Default.KeepAliveTimeout &&
            options.Resilience.KeepAliveTimeout != Timeout.InfiniteTimeSpan)
            parameters.Add($"keepAliveTimeout={(int)options.Resilience.KeepAliveTimeout.TotalSeconds}");

        if (options.Resilience.Deadline.HasValue &&
            options.Resilience.Deadline.Value != Timeout.InfiniteTimeSpan)
            parameters.Add($"defaultDeadline={(int)options.Resilience.Deadline.Value.TotalMilliseconds}");

        // Append query parameters if any
        if (parameters.Count > 0) {
            builder.Append('?');
            builder.Append(string.Join("&", parameters));
        }

        return builder.ToString();
    }
}
