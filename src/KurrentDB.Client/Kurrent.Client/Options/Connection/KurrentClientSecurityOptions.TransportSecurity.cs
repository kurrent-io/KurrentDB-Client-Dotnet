using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using OneOf;

namespace Kurrent.Client;

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
    /// Indicates whether the connection is insecure (no TLS).
    /// </summary>
    public bool IsInsecure => !IsEnabled;

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
    /// Attempts to get the root CA certificate for custom CA validation.
    /// <remarks>
    /// If the transport security is not using a custom CA file or certificate,
    /// this will false.
    /// </remarks>
    /// </summary>
    /// <param name="certificate"></param>
    /// <returns></returns>
    public bool TryGetCertificate([MaybeNullWhen(false)] out X509Certificate2 certificate) {
        certificate = Match<X509Certificate2?>(
            _ => null,
            _ => null,
            file => file.Certificate,
            cert => cert.Certificate
        );

        return certificate is not null;
    }
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

    /// <summary>
    /// The X509Certificate2 loaded from the CA file path.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public X509Certificate2 Certificate =>
        field ??= CertificateLoader.LoadCertificate(Path.GetFullPath(CaPath));
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
