using FluentValidation;

namespace Kurrent.Client;

public class KurrentClientOptionsValidator : FluentOptionsValidator<KurrentClientOptionsValidator, KurrentClientOptions> {
    public KurrentClientOptionsValidator() {
        // Connection name validation
        RuleFor(options => options.ConnectionName)
            .NotEmpty()
            .WithMessage("Connection name cannot be empty.")
            .WithSeverity(Severity.Error);

        RuleFor(options => options.ConnectionName.Length)
            .LessThanOrEqualTo(96)
            .WithMessage("Connection name exceeds 96 characters, which may impact readability in logs and diagnostics.")
            .WithSeverity(Severity.Error);

        // Direct connection endpoint validation
        When(options => options.ConnectionScheme == KurrentConnectionScheme.Direct, () => {
            RuleFor(options => options.Endpoints.Length)
                .Equal(1)
                .WithMessage("Direct connection must have exactly one endpoint.")
                .WithSeverity(Severity.Error);
        });

        // Discovery connection endpoints validation
        When(options => options.ConnectionScheme == KurrentConnectionScheme.Discover, () => {
            RuleFor(options => options.Endpoints.Length)
                .GreaterThan(0)
                .WithMessage("Discovery connection must have at least one endpoint.")
                .WithSeverity(Severity.Error);

            RuleFor(options => options.Endpoints.Length)
                .LessThanOrEqualTo(10)
                .WithMessage("More than 10 endpoints provided for discovery connection. This may impact performance.")
                .WithSeverity(Severity.Warning);
        });

        // Endpoint validation
        RuleForEach(options => options.Endpoints)
            .ChildRules(endpoint => {
                endpoint.RuleFor(ep => ep.Host)
                    .NotEmpty()
                    .WithMessage("Endpoint host cannot be empty.")
                    .WithSeverity(Severity.Error);

                endpoint.RuleFor(ep => ep.Host)
                    .Must(host => !host.Contains(' ') && !host.Contains('\t') && !host.Any(char.IsControl))
                    .WithMessage(ctx => $"Invalid hostname in endpoint: {ctx.Host}")
                    .WithSeverity(Severity.Error);

                endpoint.RuleFor(ep => ep.Port)
                    .InclusiveBetween(1, 65535)
                    .WithMessage(ctx => $"Invalid port number in endpoint: {ctx.Port}. Port must be between 1 and 65535.")
                    .WithSeverity(Severity.Error);
            });

        RuleFor(options => options.Gossip)
            .SetValidator(KurrentClientGossipOptionsValidator.Instance);

        RuleFor(options => options.Resilience)
            .SetValidator(KurrentClientResilienceOptionsValidator.Instance);

        // RuleFor(options => options.Schema)
        //     .SetValidator(KurrentClientSchemaOptionsValidator.Instance);

        RuleFor(options => options.Security)
            .SetValidator(KurrentClientSecurityOptionsValidator.Instance);
    }
}
