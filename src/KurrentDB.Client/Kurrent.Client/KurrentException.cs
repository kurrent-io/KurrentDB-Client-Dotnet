using Humanizer;

namespace Kurrent.Client;

/// <summary>
/// Exception class used to indicate errors specific to the operation and state of the Kurrent client.
/// Provides relevant error details, including error codes, statuses, field violations, and associated metadata.
/// </summary>
[PublicAPI]
public class KurrentException : Exception {
    public KurrentException(string errorCode, string errorMessage, ErrorSeverity errorSeverity, Metadata errorData, Exception? innerException = null) : base(errorMessage, innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        ErrorCode     = errorCode;
        ErrorSeverity = errorSeverity;

        ErrorData = errorData
            .With(nameof(ErrorCode), errorCode)
            .With(nameof(ErrorSeverity), errorSeverity)
            .Lock();
    }

    public KurrentException(string errorMessage, ErrorSeverity errorSeverity, Metadata errorData, Exception? innerException = null)
        : base(errorMessage, innerException) {

        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        ErrorCode     = GetType().Name.Replace("Exception", "").Underscore().ToUpper();
        ErrorSeverity = errorSeverity;
        ErrorData     = errorData
            .With(nameof(ErrorCode), ErrorCode)
            .With(nameof(ErrorSeverity), ErrorSeverity)
            .Lock();
    }

    public KurrentException(string errorMessage, Metadata errorData, Exception? innerException = null)
        : this(errorMessage, ErrorSeverity.Fatal, errorData, innerException) { }

    public KurrentException(string errorMessage, Exception? innerException = null)
        : this(errorMessage, ErrorSeverity.Fatal, [], innerException) { }

    /// <summary>
    /// The error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// The severity of the error, indicating whether it is recoverable or fatal.
    /// </summary>
    public ErrorSeverity ErrorSeverity { get; }

    /// <summary>
    /// This information is typically used to provide more details about the error condition.
    /// It can include information like which fields were violated, the state of the system when the error occurred, etc.
    /// The data is locked to prevent further modifications after the exception is created.
    /// This ensures that the error data remains consistent and immutable once the exception is thrown.
    /// </summary>
    public Metadata ErrorData { get; }
}

public abstract class KurrentException<TError>(TError error) : KurrentException(
    error.ErrorCode, error.ErrorMessage, error.ErrorSeverity, error.ErrorData
) where TError : KurrentResultError<TError> {
    public TError Error { get; } = error;
}
