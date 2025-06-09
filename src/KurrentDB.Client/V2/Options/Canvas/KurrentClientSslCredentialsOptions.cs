// namespace Kurrent.Client;
//
// /// <summary>
// /// Configures SSL/TLS credentials for secure connections to KurrentDB.
// /// </summary>
// /// <remarks>
// /// <para>
// /// This record provides configuration options for establishing secure connections
// /// using SSL/TLS with mutual authentication (mTLS) capabilities.
// /// </para>
// /// <para>
// /// The options allow configuration of client-side certificates for authentication
// /// to the server and root CA certificates for server validation.
// /// </para>
// /// </remarks>
// /// <example>
// /// <code>
// /// var sslOptions = new KurrentClientSslCredentialsOptions {
// ///     ClientCertificatePath = "/path/to/client.crt",
// ///     ClientCertificateKeyPath = "/path/to/client.key",
// ///     RootCertificatePath = "/path/to/ca.crt",
// ///     VerifyServerCertificate = true
// /// };
// /// </code>
// /// </example>
// public record KurrentClientSslCredentialsOptions {
// 	/// <summary>
// 	/// Path to the client certificate file for mTLS.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// This certificate authenticates the client to the KurrentDB server when using mutual TLS (mTLS).
// 	/// </para>
// 	/// <para>
// 	/// Must be used together with <see cref="ClientCertificateKeyPath"/> to establish a valid client identity.
// 	/// </para>
// 	/// </remarks>
// 	/// <example>
// 	/// <code>
// 	/// options.ClientCertificatePath = "/path/to/client.crt";
// 	/// </code>
// 	/// </example>
// 	public string? ClientCertificatePath { get; init; }
//
// 	/// <summary>
// 	/// Path to the client private key file for mTLS.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// The private key corresponding to the client certificate specified in <see cref="ClientCertificatePath"/>.
// 	/// </para>
// 	/// <para>
// 	/// This key should be properly secured with appropriate file permissions.
// 	/// </para>
// 	/// </remarks>
// 	/// <example>
// 	/// <code>
// 	/// options.ClientCertificateKeyPath = "/path/to/client.key";
// 	/// </code>
// 	/// </example>
// 	public string? ClientCertificateKeyPath { get; init; }
//
// 	/// <summary>
// 	/// Path to the root CA certificate for server validation.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// This certificate is used to validate the identity of the KurrentDB server.
// 	/// </para>
// 	/// <para>
// 	/// When connecting to a self-signed or custom CA environment, this should point to
// 	/// the root Certificate Authority certificate.
// 	/// </para>
// 	/// </remarks>
// 	/// <example>
// 	/// <code>
// 	/// options.RootCertificatePath = "/path/to/ca.crt";
// 	/// </code>
// 	/// </example>
// 	public string? RootCertificatePath { get; init; }
//
// 	/// <summary>
// 	/// Determines whether to verify the server certificate during TLS handshake.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// When set to <see langword="true"/> (default), the client will validate the server's certificate.
// 	/// </para>
// 	/// <para>
// 	/// <b>Security Warning:</b>
// 	/// <list type="bullet">
// 	///   <item><description>Disabling certificate verification is not recommended for production environments.</description></item>
// 	///   <item><description>Only set to <see langword="false"/> for development or testing purposes.</description></item>
// 	/// </list>
// 	/// </para>
// 	/// </remarks>
// 	/// <example>
// 	/// <code>
// 	/// // For development only:
// 	/// options.VerifyServerCertificate = false;
// 	///
// 	/// // For production (default):
// 	/// options.VerifyServerCertificate = true;
// 	/// </code>
// 	/// </example>
// 	public bool VerifyServerCertificate { get; init; } = true;
//
// 	/// <summary>
// 	/// Indicates whether a client certificate is available based on the presence of paths for the certificate
// 	/// and its associated key.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// Returns <see langword="true"/> if both the client certificate path and key path are specified.
// 	/// </para>
// 	/// <para>
// 	/// Both <see cref="ClientCertificatePath"/> and <see cref="ClientCertificateKeyPath"/> must be non-empty
// 	/// for a client certificate to be considered available.
// 	/// </para>
// 	/// </remarks>
// 	/// <returns>
// 	/// <see langword="true"/> if client certificate information is provided; otherwise, <see langword="false"/>.
// 	/// </returns>
// 	public bool HasClientCertificate =>
// 		!string.IsNullOrWhiteSpace(ClientCertificatePath) && !string.IsNullOrWhiteSpace(ClientCertificateKeyPath);
//
// 	/// <summary>
// 	/// Indicates whether a root certificate is specified for the connection.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// Returns <see langword="true"/> if the path to the root certificate is provided.
// 	/// </para>
// 	/// <para>
// 	/// This is commonly used when connecting to servers with self-signed certificates
// 	/// or certificates signed by private CAs.
// 	/// </para>
// 	/// </remarks>
// 	/// <returns>
// 	/// <see langword="true"/> if root certificate path is provided; otherwise, <see langword="false"/>.
// 	/// </returns>
// 	public bool HasRootCertificate =>
// 		!string.IsNullOrWhiteSpace(RootCertificatePath);
//
// 	/// <summary>
// 	/// Indicates whether SSL credentials are required based on the presence of client or root certificates.
// 	/// </summary>
// 	/// <remarks>
// 	/// <para>
// 	/// Returns <see langword="true"/> if either client certificates or root certificates are configured.
// 	/// </para>
// 	/// <para>
// 	/// When this property returns <see langword="true"/>, the connection will be established using SSL/TLS.
// 	/// </para>
// 	/// </remarks>
// 	/// <returns>
// 	/// <see langword="true"/> if SSL credentials should be used for the connection; otherwise, <see langword="false"/>.
// 	/// </returns>
// 	public bool Required => HasClientCertificate || HasRootCertificate;
// }
