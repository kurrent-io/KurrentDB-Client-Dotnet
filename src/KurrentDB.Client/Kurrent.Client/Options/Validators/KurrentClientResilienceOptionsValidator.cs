using FluentValidation;
using Humanizer;

namespace Kurrent.Client;

public class KurrentClientResilienceOptionsValidator : FluentOptionsValidator<KurrentClientResilienceOptionsValidator, KurrentClientResilienceOptions> {

    public KurrentClientResilienceOptionsValidator() {
        // KeepAliveInterval validation
        RuleFor(options => options.KeepAliveInterval)
            .Must(interval => interval == Timeout.InfiniteTimeSpan || interval >= TimeSpan.FromSeconds(1))
            .WithMessage("KeepAliveInterval must be at least 1 second or Timeout.InfiniteTimeSpan.")
            .WithSeverity(Severity.Error);

        // KeepAliveTimeout validation
        When(options => options.KeepAliveTimeout != Timeout.InfiniteTimeSpan && options.KeepAliveInterval != Timeout.InfiniteTimeSpan, () => {
            RuleFor(options => options.KeepAliveTimeout)
                .LessThan(options => options.KeepAliveInterval)
                .WithMessage("KeepAliveTimeout must be less than KeepAliveInterval.")
                .WithSeverity(Severity.Error);
        });

        // RetryOptions validation
        When(options => options.Retry.Enabled, () => {
            // MaxAttempts validation
            RuleFor(options => options.Retry.MaxAttempts)
                .Must(attempts => attempts > 0 || attempts == -1)
                .WithMessage("When retry is enabled, MaxAttempts must be greater than 0 or -1 for infinite retries.")
                .WithSeverity(Severity.Error);

            // BackoffMultiplier validation
            RuleFor(options => options.Retry.BackoffMultiplier)
                .GreaterThanOrEqualTo(1.0)
                .WithMessage("BackoffMultiplier must be greater than or equal to 1.0.")
                .WithSeverity(Severity.Error);

            // InitialBackoff validation
            RuleFor(options => options.Retry.InitialBackoff)
                .GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(10))
                .WithMessage("InitialBackoff must be at least 10ms.")
                .WithSeverity(Severity.Error);

            // RetryableStatusCodes validation
            RuleFor(options => options.Retry.RetryableStatusCodes)
                .Must(codes => codes.Length > 0)
                .WithMessage("RetryableStatusCodes cannot be empty when retry is enabled.")
                .WithSeverity(Severity.Error);
        });

        // Deadline consistency warning
        When(options =>
            options.Deadline.HasValue &&
            options.Deadline.Value != Timeout.InfiniteTimeSpan &&
            options.KeepAliveTimeout != Timeout.InfiniteTimeSpan, () =>
        {
            RuleFor(options => options.Deadline)
                .Must((options, deadline) => deadline!.Value >= options.KeepAliveTimeout)
                .WithMessage(options => $"Deadline ({options.Deadline!.Value.Humanize()}) is less than KeepAliveTimeout ({options.KeepAliveTimeout.Humanize()}). This may result in operations failing before keepalive detection occurs.")
                .WithSeverity(Severity.Warning);
        });
    }
}
