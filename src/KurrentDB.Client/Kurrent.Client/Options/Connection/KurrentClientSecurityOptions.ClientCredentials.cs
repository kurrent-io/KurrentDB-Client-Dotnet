using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using OneOf;

namespace Kurrent.Client;

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


    /// <summary>
    /// Attempts to get the root CA certificate for custom CA validation.
    /// <remarks>
    /// If the client credentials is not using a certificate file or certificate,
    /// this will return false.
    /// </remarks>
    /// </summary>
    public bool TryGetCertificate([MaybeNullWhen(false)] out X509Certificate2 certificate) {
        certificate = Match<X509Certificate2?>(
            _ => null,
            _ => null,
            file => file.Certificate,
            cert => cert.Certificate
        );

        return certificate is not null;
    }

    public bool IsCertificateBased => IsT2 || IsT3;

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

    /// <summary>
    /// Client certificate for authentication.
    /// </summary>
    [field: AllowNull, MaybeNull]
    public X509Certificate2 Certificate =>
        field ??= CertificateLoader.LoadCertificate(
            Path.GetFullPath(CertificatePath),
            Path.GetFullPath(KeyPath));
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
