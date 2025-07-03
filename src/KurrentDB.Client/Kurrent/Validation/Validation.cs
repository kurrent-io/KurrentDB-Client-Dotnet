using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Humanizer;

namespace Kurrent.Validation;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct ValidationSuccess {
    public static readonly ValidationSuccess Instance = new();
}

public interface IValidator<in T> where T : class {
    Result<ValidationSuccess, ValidationError> ValidateOptions(T instance);
}

[DebuggerDisplay("{ErrorCode} | {ErrorMessage}")]
public readonly record struct ValidationError(string ErrorCode, string ErrorMessage, params ValidationFailure[] Failures) : IResultError {
    public string              ErrorCode    { get; } = ErrorCode;
    public string              ErrorMessage { get; } = ErrorMessage;
    public ValidationFailure[] Failures     { get; } = Failures;

    public Exception CreateException(Exception? innerException = null) =>
        throw new ValidationException(ErrorMessage, Failures);

    public static ValidationError Create(string errorCode, params ValidationFailure[] errors) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(errors.Length, 0);
        return new ValidationError(errorCode, BuildErrorMessage(), errors);

       string BuildErrorMessage() {
            var arr = errors.Select(x => $"{Environment.NewLine} -- {x.PropertyName}: {x.ErrorMessage} Severity: {x.Severity.ToString()}");
            return $"Validation failed: {string.Join(string.Empty, arr)}";
        }
    }
}

public record ValidationFailure {
    /// <summary>
    /// The name of the property.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The error message
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public required string ErrorCode { get; init; } = "";

    /// <summary>
    /// The property value that caused the failure.
    /// </summary>
    [MaybeNull]
    public object AttemptedValue { get; init; }

    /// <summary>
    /// Custom severity level associated with the failure.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Creates a textual representation of the failure.
    /// </summary>
    public override string ToString() => ErrorMessage;

    public static ValidationFailure Create(string propertyName, string errorMessage, string? errorCode = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ValidationFailure {
            PropertyName = propertyName,
            ErrorMessage = errorMessage,
            ErrorCode    = errorCode ?? $"INVALID_{propertyName.Underscore().ToUpperInvariant()}"
        };
    }
}

public enum ValidationSeverity {
    Error,
    Warning,
    Info
}

public class ValidationException : Exception {
    public ValidationException(string message, params ValidationFailure[] errors) : base(message) {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(errors.Length, 0);
        Errors = errors;
    }

    public ValidationFailure[] Errors { get; }
}
