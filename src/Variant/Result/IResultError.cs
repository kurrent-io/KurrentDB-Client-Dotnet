namespace Kurrent;

/// <summary>
/// Contract for types that can provide structured error information to Result types.
/// Implementing this interface enables automatic exception creation and throwing capabilities
/// for Result&lt;TValue, TError&gt; where TError implements this interface.
/// </summary>
[PublicAPI]
public interface IResultError {
    /// <summary>
    /// Gets the unique code identifying the error.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// Gets the descriptive message for the error.
    /// </summary>
    string ErrorMessage { get; }

    /// <summary>
    /// Creates an exception from this error.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>An exception that represents this error.</returns>
    Exception CreateException(Exception? innerException = null);

    /// <summary>
    /// Creates and throws an exception from any IResultError.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception and does not return a value.</returns>
    /// <exception cref="Exception">Always thrown by this method, using the error's CreateException() method.</exception>
    Exception Throw(Exception? innerException = null) =>
        throw CreateException(innerException);
}
