using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Humanizer;

namespace Kurrent.Validation;

[PublicAPI]
[DebuggerDisplay("{ErrorCode} | {ErrorMessage}")]
public readonly record struct ValidationError : IResultError {
    public static readonly ValidationError None = new();

    /// <summary>
    /// The error code that categorizes the validation error.
    /// </summary>
    public string ErrorCode { get; private init; }

    /// <summary>
    /// The error message that describes the validation error.
    /// </summary>
    public string ErrorMessage { get; private init; }

    /// <summary>
    /// The validation failures that occurred during the validation process.
    /// </summary>
    public ValidationFailure[] Failures { get; private init; }

    public Exception CreateException(Exception? innerException = null) =>
        throw new ValidationException(ErrorCode, ErrorMessage, Failures);

    public static ValidationError Create(string errorCode, params ValidationFailure[] errors) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(errors.Length, 0);

        return new ValidationError {
            ErrorCode    = errorCode,
            ErrorMessage = BuildErrorMessage(),
            Failures     = errors
        };

        string BuildErrorMessage() {
            var arr = errors.Select(x => $"{Environment.NewLine} -- {x.PropertyName}: {x.ErrorMessage}");
            return $"Validation failed: {string.Join(string.Empty, arr)}";
        }
    }
}

[PublicAPI]
[DebuggerDisplay("{ErrorCode} | {ErrorMessage}")]
public readonly record struct ValidationFailure {
    public static readonly ValidationFailure None = new();

    /// <summary>
    /// The error code associated with the validation failure.
    /// This can be used to categorize or identify the type of validation error.
    /// </summary>
    public string ErrorCode { get; private init; }

    /// <summary>
    /// The error message describing the validation failure.
    /// </summary>
    public string ErrorMessage { get; private init; }

    /// <summary>
    /// The name of the property that caused the validation failure.
    /// </summary>
    public string PropertyName { get; private init; }

    /// <summary>
    /// The property value that caused the failure.
    /// </summary>
    [MaybeNull]
    public object AttemptedValue { get; private init; }

    /// <summary>
    /// Indicates whether this validation failure is associated with a specific property.
    /// </summary>
    public bool HasPropertyName => !string.IsNullOrWhiteSpace(PropertyName);

    /// <summary>
    /// Indicates whether this validation failure has an associated error code.
    /// </summary>
    public bool HasAttemptedValue => AttemptedValue is not null;

    public override string ToString() => ErrorMessage;

    /// <summary>
    /// Creates a new <see cref="ValidationFailure"/> for a specific property.
    /// </summary>
    public static ValidationFailure CreateForProperty(string propertyName, string errorMessage, object? attemptedValue = null, string? errorCode = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ValidationFailure {
            PropertyName   = propertyName,
            ErrorMessage   = errorMessage,
            AttemptedValue = attemptedValue!,
            ErrorCode      = string.IsNullOrWhiteSpace(errorCode) ? $"INVALID_{propertyName.Underscore().ToUpperInvariant()}" : errorCode
        };
    }

    /// <summary>
    /// Creates a new <see cref="ValidationFailure"/> without a specific property.
    /// This is useful for general validation errors that do not pertain to a specific property.
    /// </summary>
    public static ValidationFailure Create(string errorCode, string errorMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ValidationFailure {
            ErrorCode    = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

public interface IValidator<in T> where T : class {
    Result<ValidationSuccess, ValidationError> ValidateOptions(T instance);
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct ValidationSuccess {
    public static readonly ValidationSuccess Instance = new();
}

[PublicAPI]
public class ValidationException : Exception {
    public ValidationException(string errorCode, string message, params ValidationFailure[] errors) : base(message) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(errors.Length, 0);

        ErrorCode = errorCode;
        Errors    = errors;
    }

    public string              ErrorCode { get; }
    public ValidationFailure[] Errors    { get; }
}
