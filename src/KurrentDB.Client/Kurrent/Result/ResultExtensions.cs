namespace Kurrent;

[PublicAPI]
public static class ResultExtensions {
    /// <summary>
    /// Throws an exception if the result represents an error; otherwise, returns the success value.
    /// </summary>
    /// <param name="result">The result instance to check for success or error.</param>
    /// <param name="exceptionFactory">A function that takes the error value and returns the exception to be thrown.</param>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TException">The type of the exception to throw.</typeparam>
    /// <returns>The success value if the result represents success.</returns>
    /// <exception sref="TException">Thrown when the result represents an error.</exception>
    public static TSuccess ThrowOnError<TSuccess, TError, TException>(this Result<TSuccess, TError> result, Func<TError, TException> exceptionFactory) where TException : Exception =>
        result.IsError ? throw exceptionFactory(result.AsError) : result.AsSuccess;

    /// <summary>
    /// Throws an exception if the result represents an error; otherwise, returns the success value.
    /// </summary>
    /// <param name="result">The result instance to check for success or error.</param>
    /// <param name="exceptionFactory">A function that takes the error value and returns the exception to be thrown.</param>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <returns>The success value if the result represents success.</returns>
    /// <exception cref="Exception">Thrown when the result represents an error.</exception>
    public static TSuccess ThrowOnError<TSuccess, TError>(this Result<TSuccess, TError> result, Func<TError, Exception> exceptionFactory) =>
        result.IsError ? throw exceptionFactory(result.AsError) : result.AsSuccess;
}
