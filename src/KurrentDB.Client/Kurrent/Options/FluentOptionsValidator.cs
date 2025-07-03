using FluentValidation;
using Kurrent.Validation;

namespace Kurrent;

[PublicAPI]
public abstract class FluentOptionsValidator<TValidator, TOptions> : AbstractValidator<TOptions>, IOptionsValidator<TOptions> where TOptions : class, IOptions where TValidator : new() {
    public static readonly TValidator Instance = new TValidator();

    public virtual string ErrorCode { get; } = $"INVALID_{nameof(TOptions).ToUpperInvariant()}";

    public Result<ValidationSuccess, ValidationError> ValidateOptions(TOptions instance) {
        var validationResult = Validate(instance);
        if (validationResult.IsValid)
            return ValidationSuccess.Instance;

        var failures = validationResult.Errors
            .Select(error => new ValidationFailure {
                PropertyName   = error.PropertyName,
                ErrorMessage   = error.ErrorMessage,
                ErrorCode      = error.ErrorCode ?? ErrorCode,
                AttemptedValue = error.AttemptedValue,
                Severity       = (ValidationSeverity)error.Severity
            })
            .ToArray();

        return ValidationError.Create(ErrorCode, failures);
    }
}
