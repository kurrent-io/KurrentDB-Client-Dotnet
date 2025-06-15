using System.Reflection;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Kurrent.Client;

public abstract record KurrentClientOptionsBase {
    Lazy<dynamic> LazyValidator => new(() => {
        var validatorTypeName = $"{GetType().FullName}Validator";

        var validatorType = SystemTypes
            .ResolveTypeOrThrow(validatorTypeName);

        var validator = validatorType
            .GetField("Instance", BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);

        return validator ?? throw new InvalidOperationException($"Static `Instance` property not found on {validatorTypeName}?! Please put it there dude!");
    });

    dynamic Validator => LazyValidator.Value;

    /// <summary>
    /// Validates the configuration and returns a validation result.
    /// </summary>
    /// <returns>A validation result containing any validation errors.</returns>
    public ValidationResult ValidateConfig() => Validator.Validate((dynamic)this);

    /// <summary>
    /// Ensures the configuration is valid, throwing an exception if validation fails.
    /// </summary>
    /// <exception cref="ValidationException">
    /// Thrown when validation fails.
    /// </exception>
    public void EnsureConfigIsValid() {
        var result = (ValidationResult)Validator.Validate((dynamic)this);
        var isValid = result.Errors.Any(error => error.Severity < FluentValidation.Severity.Warning);
        if (isValid) throw new ValidationException(result.Errors);
    }
}
