namespace Kurrent.Client.Model;

/// <summary>
/// Represents a contract for Kurrent client-specific errors.
/// Extends IResultError with default KurrentClientException creation behavior.
/// </summary>
[PublicAPI]
public interface IKurrentClientError : IResultError {
    // ErrorCode and ErrorMessage inherited from IResultError

    /// <summary>
    /// Default implementation creates a KurrentClientException.
    /// This can be overridden by implementing types if different exception behavior is needed.
    /// </summary>
    Exception IResultError.CreateException(Exception? innerException) =>
        new KurrentClientException(ErrorCode, ErrorMessage, innerException);
}

/// <summary>
/// Provides extension methods for working with IKurrentClientError.
/// </summary>
[PublicAPI]
public static class KurrentClientErrorExtensions {
    /// <summary>
    /// Creates and throws an exception from any IKurrentClientError.
    /// </summary>
    /// <param name="error">The result error to convert to an exception and throw.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception and does not return a value.</returns>
    /// <exception cref="Exception">Always thrown by this method, using the error's CreateException() method.</exception>
    public static Exception Throw(this IKurrentClientError error, Exception? innerException = null) =>
        throw error.CreateException(innerException);
}
