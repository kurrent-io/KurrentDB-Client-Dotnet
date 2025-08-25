using FluentValidation;
using Humanizer;
using Kurrent.Validation;
using static System.String;

namespace Kurrent;

[PublicAPI]
public abstract class FluentOptionsValidator<TValidator, TOptions> : AbstractValidator<TOptions>, IOptionsValidator<TValidator, TOptions> where TOptions : class, IOptions where TValidator : IOptionsValidator<TValidator, TOptions>, new() {
    public static readonly TValidator Instance = new TValidator();

    public virtual string ErrorCode => $"INVALID_{nameof(TOptions).Underscore().ToUpperInvariant()}";

    public Result<ValidationSuccess, ValidationError> ValidateOptions(TOptions instance) {
        var validationResult = Validate(instance);

        // ignore warnings
        var isValid = validationResult.IsValid
                   || validationResult.Errors.All(x => x.Severity != Severity.Error);

        if (isValid)
            return ValidationSuccess.Instance;

        // we need to find a way to get these warnings and log them.
        // perhaps the success result should contain a list of warnings?
        // success would a ValidationError but with warnings lol XD

        var failures = validationResult.Errors
            .Where(err => err.Severity == Severity.Error)
            .Select(err => IsNullOrWhiteSpace(err.PropertyName)
                ? ValidationFailure.Create(err.ErrorCode,err.ErrorMessage)
                : ValidationFailure.CreateForProperty(err.PropertyName, err.ErrorMessage, err.AttemptedValue, err.ErrorCode))
            .ToArray();

        return ValidationError.Create(ErrorCode, failures);
    }
}
