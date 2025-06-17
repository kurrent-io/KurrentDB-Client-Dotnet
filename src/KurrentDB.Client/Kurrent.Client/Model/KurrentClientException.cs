using Kurrent.Variant;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents a client-specific exception that encapsulates error details
/// and additional context for exceptions occurring within the Kurrent client.
/// </summary>
[PublicAPI]
public class KurrentClientException(string errorCode, string message, Exception? innerException = null) : Exception(message, innerException) {
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; } = errorCode;

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> with an error code derived from the type name of <typeparamref name="T"/>
    /// and a message from the string representation of the <paramref name="error"/> object.
    /// </summary>
    /// <typeparam name="T">The type of the error object. The name of this type will be used as the error code.</typeparam>
    /// <param name="error">The error object. Its string representation will be used as the exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    public static KurrentClientException Throw<T>(T error, Exception? innerException = null) where T : notnull =>
        throw new KurrentClientException(typeof(T).Name, error.ToString()!, innerException);

    /// <summary>
    /// Creates a <see cref="KurrentClientException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>A new instance of <see cref="KurrentClientException"/> representing the unknown error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentClientException CreateUnknown(string operation, Exception innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        return new("Unknown", $"Unexpected error on {operation}: {innerException.Message}", innerException);
    }

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentClientException ThrowUnknown(string operation, Exception innerException) =>
        throw CreateUnknown(operation, innerException);
}

public static class KurrentClientExceptionExtensions {
    public static Exception ToException(this IVariant variantError, Exception? innerException = null) {
        if (variantError.Value is IResultError error)
            return error.CreateException(innerException);

        var invalidEx = new InvalidOperationException(
            $"The error value is not a KurrentClientErrorDetails instance but rather " +
            $"{variantError.Value.GetType().FullName}", innerException);

        return KurrentClientException.CreateUnknown("KurrentClientExceptionExtensions.Throw", invalidEx);
    }

    public static Exception Throw(this IVariant variantError, Exception? innerException = null) =>
        throw variantError.ToException(innerException);
}
