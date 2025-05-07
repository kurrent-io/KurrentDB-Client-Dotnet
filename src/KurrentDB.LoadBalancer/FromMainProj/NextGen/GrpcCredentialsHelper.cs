using Grpc.Core;

namespace KurrentDb.Client;

public static class GrpcCredentialsHelper {
	/// <summary>
	/// Creates Grpc.Core.SslCredentials based on file paths and verification options.
	/// Configures mutual TLS (mTLS) if both userCertFile and userKeyFile are provided.
	/// Configures server certificate validation based on tlsCaFile and tlsVerifyCert.
	/// </summary>
	/// <param name="userCertFile">Optional. Path to the client's certificate PEM file for mTLS.</param>
	/// <param name="userKeyFile">Optional. Path to the client's private key PEM file for mTLS.</param>
	/// <param name="tlsCaFile">Optional. Path to a custom root CA certificate PEM file for server certificate validation.</param>
	/// <param name="tlsVerifyCert">
	/// If true (default), validates the server certificate using tlsCaFile (if provided) or the system trust store.
	/// If false, **disables server certificate validation entirely**. This is insecure and shouldn't be used in production.
	/// </param>
	/// <param name="cancellationToken">Cancellation token to cancel the asynchronous operation.</param>
	public static async ValueTask<SslCredentials> CreateSslCredentialsAsync(string? userCertFile = null, string? userKeyFile = null, string? tlsCaFile = null, bool tlsVerifyCert = true, CancellationToken cancellationToken = default) {
		string?             rootCaPem            = null;
		KeyCertificatePair? clientKeyCertPair    = null;

		// Configure Client Certificate (mTLS)
		var hasCert = !string.IsNullOrEmpty(userCertFile);
		var hasKey  = !string.IsNullOrEmpty(userKeyFile);

		// Throw if only one of cert/key is provided
		if (hasCert != hasKey)
			throw new ArgumentException("Both userCertFile and userKeyFile must be provided together for client authentication (mTLS), or both must be omitted.");

		if (hasCert && hasKey) {
			if (!File.Exists(userCertFile!))
				throw new FileNotFoundException($"Client certificate file not found: {userCertFile}", userCertFile);

			if (!File.Exists(userKeyFile!))
				throw new FileNotFoundException($"Client key file not found: {userKeyFile}", userKeyFile);

			try {
				var clientCertPem = await File
					.ReadAllTextAsync(userCertFile, cancellationToken)
					.ConfigureAwait(false);

				var clientKeyPem  = await File
					.ReadAllTextAsync(userKeyFile, cancellationToken)
					.ConfigureAwait(false);

				clientKeyCertPair = new KeyCertificatePair(clientCertPem, clientKeyPem);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException) {
				throw new IOException($"Error reading client certificate or key file: {ex.Message}", ex);
			}
		}

		// Configure Server Certificate Validation
		if (!string.IsNullOrEmpty(tlsCaFile)) {
			if (!File.Exists(tlsCaFile))
				throw new FileNotFoundException($"CA certificate file not found: {tlsCaFile}", tlsCaFile);

			try {
				rootCaPem = await File
					.ReadAllTextAsync(tlsCaFile, cancellationToken)
					.ConfigureAwait(false);
			}
			catch (Exception ex) {
				throw new IOException($"Error reading CA certificate file '{tlsCaFile}': {ex.Message}", ex);
			}
		}

		// If rootCaPem remains null after this, SslCredentials will use the system trust store.
		// If verificationCallback remains null, default validation (using rootCaPem or system store) occurs.

		return new SslCredentials(rootCaPem, clientKeyCertPair, verifyPeerCallback: !tlsVerifyCert ? _ => true : null);
	}

	/// <summary>
	/// Creates Grpc.Core.SslCredentials based on file paths and verification options.
	/// Configures mutual TLS (mTLS) if both userCertFile and userKeyFile are provided.
	/// Configures server certificate validation based on tlsCaFile and tlsVerifyCert.
	/// </summary>
	/// <param name="userCertFile">Optional. Path to the client's certificate PEM file for mTLS.</param>
	/// <param name="userKeyFile">Optional. Path to the client's private key PEM file for mTLS.</param>
	/// <param name="tlsCaFile">Optional. Path to a custom root CA certificate PEM file for server certificate validation.</param>
	/// <param name="tlsVerifyCert">
	/// If true (default), validates the server certificate using tlsCaFile (if provided) or the system trust store.
	/// If false, **disables server certificate validation entirely**. This is insecure and shouldn't be used in production.
	/// </param>
	public static SslCredentials CreateSslCredentials(string? userCertFile = null, string? userKeyFile = null, string? tlsCaFile = null, bool tlsVerifyCert = true) =>
		CreateSslCredentialsAsync(userCertFile, userKeyFile, tlsCaFile, tlsVerifyCert).AsTask().GetAwaiter().GetResult();
}
