using System.Reflection;
using KurrentDB.Client;
using ValidationException = FluentValidation.ValidationException;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Kurrent.Client;

// static class ValidatorExtensions {
//     public static ValidationResult ThrowOnFailure(this ValidationResult result) =>
//         !result.IsValid ? throw new ValidationException(result.Errors) : result;
// }

public abstract record KurrentClientOptionsBase {
    Lazy<dynamic> LazyValidator => new(() => {
        var validatorTypeName = $"{GetType().FullName}Validator";

        var validatorType = SystemTypes
            .ResolveTypeOrThrow(validatorTypeName);

        var validator = validatorType
            .GetField("Instance", BindingFlags.Public | BindingFlags.Static)?
            .GetValue(null);

        return validator ?? throw new InvalidOperationException($"Static `Instance` property not found on {validatorTypeName}?!");
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
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
