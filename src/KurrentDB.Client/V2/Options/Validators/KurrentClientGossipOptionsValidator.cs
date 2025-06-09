using FluentValidation;

namespace Kurrent.Client;

class KurrentClientGossipOptionsValidator : AbstractValidator<KurrentClientGossipOptions> {
    public static readonly KurrentClientGossipOptionsValidator Instance = new();

    public KurrentClientGossipOptionsValidator() {
        // MaxDiscoverAttempts validation
        RuleFor(options => options.MaxDiscoverAttempts)
            .Must(attempts => attempts > 0 || attempts == -1)
            .WithMessage("MaxDiscoverAttempts must be greater than 0 or -1 for infinite retries.")
            .WithSeverity(Severity.Error);

        // DiscoveryInterval validation
        RuleFor(options => options.DiscoveryInterval)
            .GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(10))
            .WithMessage("DiscoveryInterval must be at least 10ms.")
            .WithSeverity(Severity.Error);

        RuleFor(options => options.DiscoveryInterval)
            .LessThanOrEqualTo(TimeSpan.FromSeconds(60))
            .WithMessage("DiscoveryInterval must not exceed 60 seconds.")
            .WithSeverity(Severity.Error);

        // Timeout validation
        RuleFor(options => options.Timeout)
            .GreaterThan(options => options.DiscoveryInterval)
            .WithMessage("Timeout must be greater than DiscoveryInterval.")
            .WithSeverity(Severity.Error);

        RuleFor(options => options.Timeout)
            .GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(100))
            .WithMessage("Timeout must be at least 100ms.")
            .WithSeverity(Severity.Error);

        RuleFor(options => options.Timeout)
            .Must(timeout => timeout <= TimeSpan.FromSeconds(60))
            .WithMessage("Timeout exceeds 60 seconds. This may result in delayed failure detection.")
            .WithSeverity(Severity.Warning);
    }
}
