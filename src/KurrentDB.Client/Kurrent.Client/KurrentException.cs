// ReSharper disable InvertIf

namespace Kurrent.Client;

/// <summary>
/// Exception class used to indicate errors specific to the operation and state of the Kurrent client.
/// Provides relevant error details, including error codes, statuses, field violations, and associated metadata.
/// </summary>
[PublicAPI]
public class KurrentException(string errorCode, string message, Metadata? metadata = null, Exception? innerException = null) : Exception(message, innerException) {
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; } = errorCode;

    /// <summary>
    /// Additional context about the error.
    /// </summary>
    public Metadata Metadata { get; } = metadata is not null ? new(metadata) : [];

    public static KurrentException Wrap<T>(T exception, string? errorCode = null) where T : Exception =>
        new KurrentException(errorCode ?? exception.GetType().Name, exception.Message, null, exception);

    /// <summary>
    /// Creates and throws a <see cref="KurrentException"/> with an error code derived from the type name of <typeparamref name="T"/>
    /// and a message from the string representation of the <paramref name="error"/> object.
    /// </summary>
    /// <typeparam name="T">The type of the error object. The name of this type will be used as the error code.</typeparam>
    /// <param name="error">The error object. Its string representation will be used as the exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentException">Always thrown by this method.</exception>
    public static KurrentException Throw<T>(T error, Exception? innerException = null) where T : notnull =>
        throw new KurrentException(typeof(T).Name, error.ToString()!, null, innerException);

    /// <summary>
    /// Creates a <see cref="KurrentException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>A new instance of <see cref="KurrentException"/> representing the unknown error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentException CreateUnknown(string operation, Exception innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        return new("Unknown", $"Unexpected behaviour detected during {operation}: {innerException.Message}", null, innerException);
    }

    /// <summary>
    /// Creates and throws a <see cref="KurrentException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentException">Always thrown by this method.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentException ThrowUnknown(string operation, Exception innerException) =>
        throw CreateUnknown(operation, innerException);
}
