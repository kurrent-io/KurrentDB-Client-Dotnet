// using System.Diagnostics.CodeAnalysis;
// using System.Runtime.InteropServices;
// using System.Security.Cryptography.X509Certificates;
// using OneOf;
//
// namespace Kurrent.Client.Security;
//
// /// <summary>
// /// Comprehensive security configuration for KurrentDB client connections.
// /// </summary>
// /// <remarks>
// /// <para>
// /// Controls all security-related aspects of client connections including TLS/SSL settings,
// /// client authentication with certificates or username/password credentials.
// /// </para>
// /// <para>
// /// Several predefined configurations are available as static properties to cover common scenarios:
// /// <see cref="Default"/>, <see cref="Insecure"/>, <see cref="MutualTls"/>.
// /// </para>
// /// </remarks>
// [PublicAPI]
// public record KurrentClientSecurityOptions {
//     /// <summary>
//     /// Transport layer security settings.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// Controls whether TLS is enabled and how certificates are verified.
//     /// </para>
//     /// <para>
//     /// When null, uses the default TLS settings (enabled with certificate verification).
//     /// </para>
//     /// </remarks>
//     public TransportSecurityOptions Transport { get; init; } = TransportSecurityOptions.Default;
//
//     /// <summary>
//     /// Client authentication credentials.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// Specifies how the client authenticates with the server.
//     /// </para>
//     /// <para>
//     /// Can be certificate-based, username/password, or none.
//     /// </para>
//     /// </remarks>
//     public ClientCredentials Authentication { get; init; } = ClientCredentials.None;
//
//     /// <summary>
//     /// Default security settings with TLS enabled and server certificate verification.
//     /// No client authentication is configured.
//     /// </summary>
//     /// <returns>
//     /// A new <see cref="KurrentClientSecurityOptions"/> instance with secure-by-default settings.
//     /// </returns>
//     public static KurrentClientSecurityOptions Default =>
//         new() {
//             Transport      = TransportSecurityOptions.Default,
//             Authentication = ClientCredentials.None
//         };
//
//     /// <summary>
//     /// Insecure settings with TLS disabled. Only use for development.
//     /// </summary>
//     /// <returns>
//     /// A new <see cref="KurrentClientSecurityOptions"/> instance with TLS disabled.
//     /// </returns>
//     public static KurrentClientSecurityOptions Insecure =>
//         new() {
//             Transport      = TransportSecurityOptions.Insecure,
//             Authentication = ClientCredentials.None
//         };
//
//     /// <summary>
//     /// Mutual TLS authentication settings with client certificates from file paths.
//     /// </summary>
//     /// <param name="clientCertPath">Path to client certificate file</param>
//     /// <param name="clientKeyPath">Path to client private key file</param>
//     /// <param name="rootCaPath">Optional path to root CA certificate</param>
//     /// <returns>
//     /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for mutual TLS authentication.
//     /// </returns>
//     public static KurrentClientSecurityOptions MutualTls(string clientCertPath, string clientKeyPath, string? rootCaPath = null) =>
//         new() {
//             Transport = new TransportSecurityOptions {
//                 Enabled                 = true,
//                 VerifyServerCertificate = true,
//                 RootCertificatePath     = rootCaPath
//             },
//             Authentication = new CertificateFileCredentials(clientCertPath, clientKeyPath)
//         };
//
//     /// <summary>
//     /// Mutual TLS authentication settings using X509Certificate2 objects.
//     /// </summary>
//     /// <param name="clientCertificate">Client certificate</param>
//     /// <param name="rootCaCertificate">Optional root CA certificate</param>
//     /// <returns>
//     /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for mutual TLS authentication with certificate objects.
//     /// </returns>
//     public static KurrentClientSecurityOptions MutualTlsWithCertificates(
//         X509Certificate2 clientCertificate,
//         X509Certificate2? rootCaCertificate = null
//     ) =>
//         new() {
//             Transport = new TransportSecurityOptions {
//                 Enabled                 = true,
//                 VerifyServerCertificate = true,
//                 RootCertificate         = rootCaCertificate
//             },
//             Authentication = new X509CertificateCredentials(clientCertificate)
//         };
//
//     /// <summary>
//     /// Basic authentication settings with username/password. Enables TLS for secure transmission.
//     /// </summary>
//     /// <param name="username">Username for authentication</param>
//     /// <param name="password">Password for authentication</param>
//     /// <returns>
//     /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for username/password authentication.
//     /// </returns>
//     public static KurrentClientSecurityOptions Basic(string username, string password) =>
//         new() {
//             Transport      = TransportSecurityOptions.Default, // Always use TLS with password authentication
//             Authentication = new UserCredentials(username, password)
//         };
// }
//
// /// <summary>
// /// Transport layer security options for controlling TLS/SSL behavior.
// /// </summary>
// /// <remarks>
// /// <para>
// /// Controls whether TLS is enabled and how certificates are verified.
// /// </para>
// /// </remarks>
// [PublicAPI]
// public record TransportSecurityOptions {
//     /// <summary>
//     /// Whether TLS is enabled for the connection.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// When <see langword="true"/>, the connection uses TLS encryption.
//     /// </para>
//     /// <para>
//     /// Default value is <see langword="true"/>.
//     /// </para>
//     /// </remarks>
//     public bool Enabled { get; init; } = true;
//
//     /// <summary>
//     /// Whether to verify the server certificate during TLS handshake.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// When <see langword="true"/>, the client validates the server's certificate.
//     /// </para>
//     /// <para>
//     /// Setting this to <see langword="false"/> is not recommended for production.
//     /// </para>
//     /// <para>
//     /// Default value is <see langword="true"/>.
//     /// </para>
//     /// </remarks>
//     public bool VerifyServerCertificate { get; init; } = true;
//
//     /// <summary>
//     /// Path to the root CA certificate file for server validation.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// When specified, this certificate file is used to validate the server's identity.
//     /// </para>
//     /// <para>
//     /// Takes precedence over <see cref="RootCertificate"/> if both are specified.
//     /// </para>
//     /// <para>
//     /// When null, the system's default trust store is used unless <see cref="RootCertificate"/>
//     /// is provided.
//     /// </para>
//     /// </remarks>
//     public string? RootCertificatePath { get; init; }
//
//     /// <summary>
//     /// Root CA certificate for server validation.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// When specified, this certificate is used to validate the server's identity.
//     /// </para>
//     /// <para>
//     /// <see cref="RootCertificatePath"/> takes precedence if both are specified.
//     /// </para>
//     /// <para>
//     /// When null, the system's default trust store is used unless <see cref="RootCertificatePath"/>
//     /// is provided.
//     /// </para>
//     /// </remarks>
//     public X509Certificate2? RootCertificate { get; init; }
//
//     /// <summary>
//     /// Default TLS settings (enabled with certificate verification).
//     /// </summary>
//     /// <returns>
//     /// A new <see cref="TransportSecurityOptions"/> instance with secure-by-default settings.
//     /// </returns>
//     public static TransportSecurityOptions Default => new();
//
//     /// <summary>
//     /// Insecure TLS settings (disabled). Only use for development.
//     /// </summary>
//     /// <returns>
//     /// A new <see cref="TransportSecurityOptions"/> instance with TLS disabled.
//     /// </returns>
//     public static TransportSecurityOptions Insecure =>
//         new() {
//             Enabled                 = false,
//             VerifyServerCertificate = false
//         };
//
//     /// <summary>
//     /// Creates transport security options with a custom CA certificate from a file path.
//     /// </summary>
//     /// <param name="rootCaPath">Path to the root CA certificate file</param>
//     /// <returns>
//     /// A new <see cref="TransportSecurityOptions"/> instance with custom CA certificate.
//     /// </returns>
//     public static TransportSecurityOptions WithCustomCa(string rootCaPath) =>
//         new() {
//             RootCertificatePath = rootCaPath
//         };
//
//     /// <summary>
//     /// Creates transport security options with a custom CA certificate.
//     /// </summary>
//     /// <param name="rootCaCert">Root CA certificate</param>
//     /// <returns>
//     /// A new <see cref="TransportSecurityOptions"/> instance with custom CA certificate.
//     /// </returns>
//     public static TransportSecurityOptions WithCustomCa(X509Certificate2 rootCaCert) =>
//         new() {
//             RootCertificate = rootCaCert
//         };
// }
//
// [PublicAPI]
// [GenerateOneOf]
// public partial class ClientCredentials : OneOfBase<NoCredentials, UserCredentials, CertificateFileCredentials, X509CertificateCredentials> {
//     public bool IsNoCredentials              => IsT0;
//     public bool IsUserCredentials            => IsT1;
//     public bool IsCertificateFileCredentials => IsT2;
//     public bool IsX509CertificateCredentials => IsT3;
//
//     public NoCredentials              AsNoCredentials              => AsT0;
//     public UserCredentials            AsUserCredentials            => AsT1;
//     public CertificateFileCredentials AsCertificateFileCredentials => AsT2;
//     public X509CertificateCredentials AsX509CertificateCredentials => AsT3;
//
//     public static NoCredentials None => NoCredentials.Value;
// }
//
// /// <summary>
// /// Represents no authentication credentials.
// /// </summary>
// [StructLayout(LayoutKind.Sequential, Size = 1)]
// public readonly struct NoCredentials {
//     public static readonly NoCredentials Value = new();
// }
//
// /// <summary>
// /// Certificate-based authentication credentials using file paths.
// /// </summary>
// [PublicAPI]
// public record CertificateFileCredentials {
//     /// <summary>
//     /// Creates certificate-based credentials using file paths.
//     /// </summary>
//     /// <param name="certificatePath">Path to the client certificate file</param>
//     /// <param name="keyPath">Path to the client private key file</param>
//     /// <exception cref="ArgumentException">Thrown when either path is null or empty</exception>
//     public CertificateFileCredentials(string certificatePath, string keyPath) {
//         if (string.IsNullOrWhiteSpace(certificatePath))
//             throw new ArgumentException("Certificate path cannot be empty", nameof(certificatePath));
//
//         if (string.IsNullOrWhiteSpace(keyPath))
//             throw new ArgumentException("Key path cannot be empty", nameof(keyPath));
//
//         CertificatePath = certificatePath;
//         KeyPath         = keyPath;
//     }
//
//     /// <summary>
//     /// Path to the client certificate file.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// The certificate file used to authenticate the client to the server.
//     /// </para>
//     /// </remarks>
//     public string CertificatePath { get; }
//
//     /// <summary>
//     /// Path to the client private key file.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// The private key file corresponding to the client certificate.
//     /// </para>
//     /// </remarks>
//     public string KeyPath { get; }
// }
//
// /// <summary>
// /// Certificate-based authentication credentials using X509Certificate2 objects.
// /// </summary>
// [PublicAPI]
// public record X509CertificateCredentials() {
//     /// <summary>
//     /// Creates certificate-based credentials using an X509Certificate2 object.
//     /// </summary>
//     /// <param name="certificate">The client certificate for authentication</param>
//     /// <exception cref="ArgumentNullException">Thrown when certificate is null</exception>
//     [method: SetsRequiredMembers]
//     public X509CertificateCredentials(X509Certificate2 certificate) : this() =>
//         Certificate = certificate;
//
//     /// <summary>
//     /// Client certificate for authentication.
//     /// </summary>
//     /// <remarks>
//     /// <para>
//     /// The certificate used to authenticate the client to the server.
//     /// </para>
//     /// </remarks>
//     public required X509Certificate2 Certificate { get; init; }
// }
//
// /// <summary>
// /// Username/password authentication credentials.
// /// </summary>
// [PublicAPI]
// public record UserCredentials {
//     /// <summary>
//     /// Creates user credentials for authentication.
//     /// </summary>
//     /// <param name="username">Username for authentication</param>
//     /// <param name="password">Password for authentication</param>
//     /// <exception cref="ArgumentException">Thrown when username is null or empty</exception>
//     public UserCredentials(string username, string? password) {
//         if (string.IsNullOrWhiteSpace(username))
//             throw new ArgumentException("Username cannot be empty", nameof(username));
//
//         Username = username;
//         Password = password;
//     }
//
//     /// <summary>
//     /// Username for authentication.
//     /// </summary>
//     public string Username { get; }
//
//     /// <summary>
//     /// Password for authentication.
//     /// </summary>
//     public string? Password { get; }
// }
