// namespace Kurrent;
//
// [PublicAPI]
// public static class ResultThrowExtensions {
//     /// <summary>
//     /// Throws an exception if the result represents an error; otherwise, returns the success value.
//     /// </summary>
//     /// <param name="result">The result instance to check for success or error.</param>
//     /// <param name="exceptionFactory">A function that takes the error value and returns the exception to be thrown.</param>
//     /// <typeparam name="TValue">The type of the success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TException">The type of the exception to throw.</typeparam>
//     /// <returns>The success value if the result represents success.</returns>
//     /// <exception cref="TException">Thrown when the result represents an error.</exception>
//     public static TValue ThrowOnException<TValue, TError, TException>(this Result<TValue, TError> result, Func<TError, TException> exceptionFactory) where TException : Exception where TError : notnull =>
//         result.IsFailure ? throw exceptionFactory(result.Error) : result.Value;
//
//     /// <summary>
//     /// Throws an exception if the result represents an error; otherwise, returns the success value.
//     /// </summary>
//     /// <param name="result">The result instance to check for success or error.</param>
//     /// <param name="exceptionFactory">A function that takes the error value and returns the exception to be thrown.</param>
//     /// <typeparam name="TValue">The type of the success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <returns>The success value if the result represents success.</returns>
//     /// <exception cref="Exception">Thrown when the result represents an error.</exception>
//     public static TValue ThrowOnException<TValue, TError>(this Result<TValue, TError> result, Func<TError, Exception> exceptionFactory) where TError : notnull =>
//         result.IsFailure ? throw exceptionFactory(result.Error) : result.Value;
//
//     public static async ValueTask<TSuccess> ThrowOnErrorAsync<TSuccess, TError, TException>(
//         this ValueTask<Result<TSuccess, TError>> resultTask, Func<TError, TException> exceptionFactory)
//         where TException : Exception where TSuccess : notnull where TError : notnull {
//         var result = await resultTask.ConfigureAwait(false);
//         return result.ThrowOnException(exceptionFactory);
//     }
//
//     public static async ValueTask<TValue> ThrowOnExceptionAsync<TValue, TError>(
//         this ValueTask<Result<TValue, TError>> resultTask, Func<TError, Exception> exceptionFactory)
//         where TValue : notnull where TError : notnull {
//
//         var result = await resultTask.ConfigureAwait(false);
//         return result.ThrowOnException(exceptionFactory);
//     }
//
//     public static TValue ThrowOnException<TValue>(this Result<TValue, Exception> result) =>
//         result.IsFailure ? throw result.Error : result.Value;
//
//     public static async ValueTask<TValue> ThrowOnExceptionAsync<TValue>(
//         this ValueTask<Result<TValue, Exception>> resultTask)
//         where TValue : notnull {
//
//         var result = await resultTask.ConfigureAwait(false);
//         return result.ThrowOnException();
//     }
// }
