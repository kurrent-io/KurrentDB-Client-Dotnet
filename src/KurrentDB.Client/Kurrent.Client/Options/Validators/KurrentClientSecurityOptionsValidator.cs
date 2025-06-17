using FluentValidation;

namespace Kurrent.Client;

class KurrentClientSecurityOptionsValidator : AbstractValidator<KurrentClientSecurityOptions> {
    public static readonly KurrentClientSecurityOptionsValidator Instance = new();

    public KurrentClientSecurityOptionsValidator() {
        // Transport security validation
        When(options => options.Authentication.IsBasicCredentials, () => {
            RuleFor(options => options.Transport.IsEnabled)
                .Equal(true)
                .WithMessage("Using basic authentication without TLS is not secure.")
                .WithSeverity(Severity.Warning);
        });

        // Certificate path validation for Authentication
        When(options => options.Authentication.IsCertificateFileCredentials, () => {
            RuleFor(options => options.Authentication.AsFileCertificateCredentials.CertificatePath)
                .Must(Path.IsPathRooted)
                .WithMessage(options => $"Relative certificate path may not resolve correctly at runtime: {options.Authentication.AsFileCertificateCredentials.CertificatePath}")
                .WithSeverity(Severity.Warning);

            RuleFor(options => options.Authentication.AsFileCertificateCredentials.KeyPath)
                .Must(Path.IsPathRooted)
                .WithMessage(options => $"Relative key path may not resolve correctly at runtime: {options.Authentication.AsFileCertificateCredentials.KeyPath}")
                .WithSeverity(Severity.Warning);
        });

        // Certificate path validation for Transport
        When(options => options.Transport.IsFileCertificateTls, () => {
            RuleFor(options => options.Transport.AsFileCertificateTls.CaPath)
                .Must(Path.IsPathRooted)
                .WithMessage(options => $"Relative certificate path may not resolve correctly at runtime: {options.Transport.AsFileCertificateTls.CaPath}")
                .WithSeverity(Severity.Warning);
        });
    }
}
