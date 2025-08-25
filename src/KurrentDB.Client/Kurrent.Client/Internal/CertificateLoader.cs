using System.Security.Cryptography.X509Certificates;
using static System.String;

namespace Kurrent.Client;

static class CertificateLoader {
    /// <summary>
    /// Automatically detects the certificate format and loads it appropriately.
    /// <remarks>
    /// Supported formats are .pfx, .pem, .crt, .cer
    /// </remarks>
    /// </summary>
    /// <param name="certPath">Path to certificate file</param>
    /// <param name="keyPath">Optional path to private key file (for PEM certificates)</param>
    /// <param name="password">Optional password (for encrypted keys or PFX files)</param>
    public static X509Certificate2 LoadCertificate(
        string certPath,
        string? keyPath = null,
        string? password = null
    ) {
        if (IsNullOrWhiteSpace(certPath))
            throw new ArgumentException("Certificate file path cannot be empty", nameof(certPath));

        if (!File.Exists(certPath))
            throw new FileNotFoundException($"Certificate file not found: {certPath}");

        if (!IsNullOrWhiteSpace(keyPath) && !File.Exists(keyPath))
            throw new FileNotFoundException($"Private key file not found: {keyPath}", keyPath);

        var extension = Path.GetExtension(certPath).ToLowerInvariant();

        return extension switch {
            ".pfx" or ".p12"           => LoadFromPfx(certPath, password),
            ".pem" or ".crt" or ".cer" => LoadFromPem(certPath, keyPath, password),
            _                          => throw new NotSupportedException($"Unsupported certificate format: {extension}")
        };
    }

    static X509Certificate2 LoadFromPfx(string certPath, string? password = null) {
        // Use EphemeralKeySet for better security and cross-platform compatibility
        // This avoids creating key container files on disk

        // var keyStorageFlags = OperatingSystem.IsWindows()
        //     ? X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet
        //     : X509KeyStorageFlags.EphemeralKeySet;

        #if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(certPath), password, X509KeyStorageFlags.EphemeralKeySet);
        #else
        return new X509Certificate2(certPath, password, X509KeyStorageFlags.EphemeralKeySet);
        #endif
    }

    static X509Certificate2 LoadFromPem(string certPath, string? keyPath, string? password) {
        if (IsNullOrEmpty(keyPath))
        #if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificateFromFile(certPath);
        #else
        return X509Certificate2.CreateFromPemFile(certPath);
        #endif

        return IsNullOrEmpty(password)
            ? X509Certificate2.CreateFromPemFile(certPath, keyPath)
            : X509Certificate2.CreateFromEncryptedPemFile(certPath, keyPath, password);
    }
}
