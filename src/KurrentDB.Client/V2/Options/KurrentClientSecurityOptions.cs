using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using KurrentDB.Client;
using OneOf;

namespace Kurrent.Client.Security;

/// <summary>
/// Comprehensive security configuration for KurrentDB client connections.
/// </summary>
/// <remarks>
/// <para>
/// Controls all security-related aspects of client connections including TLS/SSL settings,
/// client authentication with certificates or username/password credentials.
/// </para>
/// <para>
/// Several predefined configurations are available as static properties to cover common scenarios:
/// <see cref="Default"/>, <see cref="Insecure"/>, <see cref="MutualTls"/>.
/// </para>
/// </remarks>
[PublicAPI]
public record KurrentClientSecurityOptions : KurrentClientOptionsBase {
    /// <summary>
    /// Transport layer security settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Controls whether TLS is enabled and how certificates are verified.
    /// </para>
    /// <para>
    /// When null, uses the default TLS settings (standard TLS with system trust store).
    /// </para>
    /// </remarks>
    public TransportSecurity Transport { get; init; } = TransportSecurity.Standard;

    /// <summary>
    /// Client authentication credentials.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specifies how the client authenticates with the server.
    /// </para>
    /// <para>
    /// Can be certificate-based, username/password, or none.
    /// </para>
    /// </remarks>
    public ClientCredentials Authentication { get; init; } = ClientCredentials.None;

    /// <summary>
    /// Default security settings with TLS enabled and server certificate verification.
    /// No client authentication is configured.
    /// </summary>
    /// <returns>
    /// A new <see cref="KurrentClientSecurityOptions"/> instance with secure-by-default settings.
    /// </returns>
    public static KurrentClientSecurityOptions Default =>
        new() {
            Transport      = TransportSecurity.Standard,
            Authentication = ClientCredentials.None
        };

    /// <summary>
    /// Insecure settings with TLS disabled. Only use for development.
    /// </summary>
    /// <returns>
    /// A new <see cref="KurrentClientSecurityOptions"/> instance with TLS disabled.
    /// </returns>
    public static KurrentClientSecurityOptions Insecure =>
        new() {
            Transport      = TransportSecurity.None,
            Authentication = ClientCredentials.None
        };

    /// <summary>
    /// Mutual TLS authentication settings with client certificates from file paths.
    /// </summary>
    /// <param name="clientCertPath">Path to client certificate file</param>
    /// <param name="clientKeyPath">Path to client private key file</param>
    /// <param name="rootCaPath">Optional path to root CA certificate</param>
    /// <returns>
    /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for mutual TLS authentication.
    /// </returns>
    public static KurrentClientSecurityOptions MutualTls(string clientCertPath, string clientKeyPath, string? rootCaPath = null) =>
        new() {
            Transport      = rootCaPath is not null ? TransportSecurity.Certificate(rootCaPath) : TransportSecurity.Standard,
            Authentication = new FileCertificateCredentials(clientCertPath, clientKeyPath)
        };

    /// <summary>
    /// Mutual TLS authentication settings using X509Certificate2 certificates.
    /// </summary>
    /// <param name="clientCertificate">Client certificate</param>
    /// <param name="rootCaCertificate">Optional root CA certificate</param>
    /// <returns>
    /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for mutual TLS authentication with certificate objects.
    /// </returns>
    public static KurrentClientSecurityOptions MutualTls(X509Certificate2 clientCertificate, X509Certificate2? rootCaCertificate = null) =>
        new() {
            Transport      = rootCaCertificate is not null ? TransportSecurity.Certificate(rootCaCertificate) : TransportSecurity.Standard,
            Authentication = new X509CertificateCredentials(clientCertificate)
        };

    /// <summary>
    /// Basic authentication settings with username/password. Enables TLS for secure transmission.
    /// </summary>
    /// <param name="username">Username for authentication</param>
    /// <param name="password">Password for authentication</param>
    /// <returns>
    /// A new <see cref="KurrentClientSecurityOptions"/> instance configured for username/password authentication.
    /// </returns>
    public static KurrentClientSecurityOptions Basic(string username, string password) =>
        new() {
            Transport      = TransportSecurity.Standard, // Always use TLS with password authentication
            Authentication = new BasicCredentials(username, password)
        };
}

#region . Client Credentials .

[PublicAPI]
[GenerateOneOf]
public partial class ClientCredentials : OneOfBase<NoCredentials, BasicCredentials, FileCertificateCredentials, X509CertificateCredentials> {
    public bool IsNoCredentials              => IsT0;
    public bool IsBasicCredentials           => IsT1;
    public bool IsCertificateFileCredentials => IsT2;
    public bool IsX509CertificateCredentials => IsT3;

    public NoCredentials              AsNoCredentials              => AsT0;
    public BasicCredentials           AsCredentials                => AsT1;
    public FileCertificateCredentials AsFileCertificateCredentials => AsT2;
    public X509CertificateCredentials AsX509CertificateCredentials => AsT3;

    public static NoCredentials None => NoCredentials.Value;

    public static ClientCredentials Basic(string username, string? password) =>
        new BasicCredentials(username, password);

    public static ClientCredentials Certificate(string certificatePath, string keyPath) =>
        new FileCertificateCredentials(certificatePath, keyPath);

    public static ClientCredentials Certificate(X509Certificate2 certificate) =>
        new X509CertificateCredentials(certificate);
}

/// <summary>
/// Represents no authentication credentials.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct NoCredentials {
    public static readonly NoCredentials Value = new();
}

/// <summary>
/// Certificate-based authentication credentials using file paths.
/// </summary>
[PublicAPI]
public record FileCertificateCredentials {
    /// <summary>
    /// Creates certificate-based credentials using file paths.
    /// </summary>
    /// <param name="certificatePath">Path to the client certificate file</param>
    /// <param name="keyPath">Path to the client private key file</param>
    /// <exception cref="ArgumentException">Thrown when either path is null or empty</exception>
    public FileCertificateCredentials(string certificatePath, string keyPath) {
        if (string.IsNullOrWhiteSpace(certificatePath))
            throw new ArgumentException("Certificate path cannot be empty", nameof(certificatePath));

        if (string.IsNullOrWhiteSpace(keyPath))
            throw new ArgumentException("Key path cannot be empty", nameof(keyPath));

        CertificatePath = certificatePath.UnescapeDataStringIfNeeded();
        KeyPath         = keyPath.UnescapeDataStringIfNeeded();
    }

    /// <summary>
    /// Path to the client certificate file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The certificate file used to authenticate the client to the server.
    /// </para>
    /// </remarks>
    public string CertificatePath { get; }

    /// <summary>
    /// Path to the client private key file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The private key file corresponding to the client certificate.
    /// </para>
    /// </remarks>
    public string KeyPath { get; }
}

/// <summary>
/// Certificate-based authentication credentials using X509Certificate2 objects.
/// </summary>
[PublicAPI]
public record X509CertificateCredentials() {
    /// <summary>
    /// Creates certificate-based credentials using an X509Certificate2 object.
    /// </summary>
    /// <param name="certificate">The client certificate for authentication</param>
    /// <exception cref="ArgumentNullException">Thrown when certificate is null</exception>
    [method: SetsRequiredMembers]
    public X509CertificateCredentials(X509Certificate2 certificate) : this() =>
        Certificate = certificate;

    /// <summary>
    /// Client certificate for authentication.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The certificate used to authenticate the client to the server.
    /// </para>
    /// </remarks>
    public required X509Certificate2 Certificate { get; init; }
}

/// <summary>
/// Username/password authentication credentials.
/// </summary>
[PublicAPI]
public record BasicCredentials {
    /// <summary>
    /// Creates user credentials for authentication.
    /// </summary>
    /// <param name="username">Username for authentication</param>
    /// <param name="password">Password for authentication</param>
    /// <exception cref="ArgumentException">Thrown when username is null or empty</exception>
    public BasicCredentials(string username, string? password) {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        Username = username;
        Password = password;
    }

    /// <summary>
    /// Username for authentication.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Password for authentication.
    /// </summary>
    public string? Password { get; }
}

#endregion

#region . Transport Security .

/// <summary>
/// Transport security options for connecting to KurrentDB.
/// </summary>
/// <remarks>
/// <para>
/// Controls how the client secures the communication channel with the server.
/// </para>
/// <para>
/// Supports multiple security modes: no security, standard TLS, custom certificate authority TLS,
/// and certificate-based TLS.
/// </para>
/// </remarks>
[PublicAPI]
[GenerateOneOf]
public partial class TransportSecurity : OneOfBase<NoTls, StandardTls, FileCertificateTls, X509CertificateTls> {
    public bool IsNoTransportSecurity => IsT0;
    public bool IsStandardTls         => IsT1;
    public bool IsFileCertificateTls  => IsT2;
    public bool IsX509CertificateTls  => IsT3;

    public NoTls              AsNoTls              => AsT0;
    public StandardTls        AsStandardTls        => AsT1;
    public FileCertificateTls AsFileCertificateTls => AsT2;
    public X509CertificateTls AsX509CertificateTls => AsT3;

    /// <summary>
    /// No transport security. Only use for development environments.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mode provides no encryption or authentication for the transport layer.
    /// </para>
    /// <para>
    /// <b>Warning:</b> Not recommended for production use as all data is transmitted in plaintext.
    /// </para>
    /// </remarks>
    public static TransportSecurity None => NoTls.Instance;

    /// <summary>
    /// Standard TLS security using system-trusted certificate authorities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This mode uses the system's certificate trust store to validate server certificates.
    /// </para>
    /// </remarks>
    public static TransportSecurity Standard => StandardTls.Default;

    /// <summary>
    /// Creates a transport security configuration that disables certificate validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warning:</b> Only use for development environments, as it makes the connection
    /// vulnerable to man-in-the-middle attacks.
    /// </para>
    /// </remarks>
    /// <returns>A StandardTls configuration with certificate validation disabled.</returns>
    public static TransportSecurity Insecure => StandardTls.Insecure;

    /// <summary>
    /// Creates a transport security configuration using a custom CA certificate file.
    /// </summary>
    /// <param name="caPath">Path to the CA certificate file.</param>
    /// <returns>A CertificateFileTls configuration with the specified CA certificate.</returns>
    public static TransportSecurity Certificate(string caPath) => new FileCertificateTls(caPath);

    /// <summary>
    /// Creates a transport security configuration using a custom CA certificate object.
    /// </summary>
    /// <param name="certificate">The X509Certificate2 object.</param>
    /// <returns>A CertificateObjectTls configuration with the specified certificate.</returns>
    public static TransportSecurity Certificate(X509Certificate2 certificate) => new X509CertificateTls(certificate);

    /// <summary>
    /// Indicates whether transport security is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <see langword="true"/> if any form of TLS is enabled, <see langword="false"/> for
    /// <see cref="NoTls"/>.
    /// </para>
    /// </remarks>
    public bool IsEnabled =>
        Match(
            _ => false,
            _ => true,
            _ => true,
            _ => true
        );

    /// <summary>
    /// Gets whether server certificate verification is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns <see langword="true"/> if TLS is enabled and certificate verification is enabled.
    /// </para>
    /// <para>
    /// Returns <see langword="false"/> for insecure connections or when certificate validation is disabled.
    /// </para>
    /// </remarks>
    public bool VerifyServerCertificate =>
        Match(
            _    => false,
            std  => std.VerifyServerCertificate,
            file => file.VerifyServerCertificate,
            cert => cert.VerifyServerCertificate
        );

    /// <summary>
    /// Gets the root CA certificate path for custom CA validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns the path to a custom CA certificate file if specified, or null.
    /// </para>
    /// </remarks>
    public string? GetRootCertificatePath() =>
        Match<string?>(
            _    => null,
            _    => null,
            file => file.CaPath,
            _    => null
        );

    /// <summary>
    /// Gets the root CA certificate for custom CA validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Returns a X509Certificate2 object if specified for custom CA validation, or null.
    /// </para>
    /// </remarks>
    public X509Certificate2? GetRootCertificate() =>
        Match<X509Certificate2?>(
            _    => null,
            _    => null,
            _    => null,
            cert => cert.Certificate
        );
}

/// <summary>
/// Represents no transport security (insecure connection).
/// </summary>
/// <remarks>
/// <para>
/// Use only in development environments where security is not a concern.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct NoTls {
    public static readonly NoTls Instance = new();
}

/// <summary>
/// Represents standard TLS security using system-trusted certificate authorities.
/// </summary>
public record StandardTls {
    /// <summary>
    /// Whether to verify server certificates against the system's certificate trust store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, server certificates will be validated against trusted CAs.
    /// </para>
    /// <para>
    /// Setting to <see langword="false"/> is a security risk and should only be used in development.
    /// </para>
    /// </remarks>
    public bool VerifyServerCertificate { get; init; } = true;

    /// <summary>
    /// Default StandardTls configuration with certificate validation enabled.
    /// </summary>
    public static StandardTls Default => new();

    /// <summary>
    /// StandardTls configuration with certificate validation disabled (insecure).
    /// </summary>
    public static StandardTls Insecure => new() { VerifyServerCertificate = false };
}

/// <summary>
/// Represents TLS security using a certificate authority file.
/// </summary>
public record FileCertificateTls {
    /// <summary>
    /// Creates a new CertificateFileTls instance with the specified CA path.
    /// </summary>
    /// <param name="caPath">Path to the certificate authority file.</param>
    [SetsRequiredMembers]
    public FileCertificateTls(string caPath) =>
        CaPath = caPath.UnescapeDataStringIfNeeded();

    /// <summary>
    /// Path to the certificate authority file.
    /// </summary>
    public required string CaPath { get; init; }

    /// <summary>
    /// Whether to verify server certificates against the provided CA certificate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, server certificates will be validated against the CA certificate.
    /// </para>
    /// <para>
    /// Setting to <see langword="false"/> is a security risk and should only be used in development.
    /// </para>
    /// </remarks>
    public bool VerifyServerCertificate { get; init; } = true;
}

/// <summary>
/// Represents TLS security using a certificate authority object.
/// </summary>
public record X509CertificateTls {
    /// <summary>
    /// Creates a new CertificateObjectTls instance with the specified certificate.
    /// </summary>
    /// <param name="certificate">The X509Certificate2 to use as a certificate authority.</param>
    [SetsRequiredMembers]
    public X509CertificateTls(X509Certificate2 certificate) =>
        Certificate = certificate;

    /// <summary>
    /// The X509Certificate2 to use as a certificate authority.
    /// </summary>
    public required X509Certificate2 Certificate { get; init; }

    /// <summary>
    /// Whether to verify server certificates against the provided CA certificate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true"/>, server certificates will be validated against the CA certificate.
    /// </para>
    /// <para>
    /// Setting to <see langword="false"/> is a security risk and should only be used in development.
    /// </para>
    /// </remarks>
    public bool VerifyServerCertificate { get; init; } = true;
}

#endregion
