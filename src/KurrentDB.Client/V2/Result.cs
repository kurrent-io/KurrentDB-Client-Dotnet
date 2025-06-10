using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// This class provides a functional approach to error handling, avoiding exceptions for expected failures.
/// It supports direct instantiation, inheritance for domain-specific result types, and a fluent API for transformations.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToDebugString(),nq}")]
public class Result<TSuccess, TError> {
    readonly TSuccess  _success;
    readonly TError    _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess,TError}"/> class.
    /// This constructor is protected to encourage use of static factory methods <see cref="Success(TSuccess)"/> and <see cref="Error(TError)"/>
    /// or for use by derived classes.
    /// </summary>
    /// <param name="isSuccess">A flag indicating whether the result represents a success.</param>
    /// <param name="success">The success value, applicable if <paramref name="isSuccess"/> is true. Defaults to <c>default</c>.</param>
    /// <param name="error">The error value, applicable if <paramref name="isSuccess"/> is false. Defaults to <c>default</c>.</param>
    protected Result(bool isSuccess, TSuccess? success = default, TError? error = default) {
        IsSuccess = isSuccess;
        if (isSuccess) {
            _success = success!;
            _error   = default!;
        } else {
            _success = default!;
            _error   = error!;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    /// <value><c>true</c> if the operation was successful; otherwise, <c>false</c>.</value>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    /// <value><c>true</c> if the operation failed; otherwise, <c>false</c>.</value>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not a success.
    /// </summary>
    /// <value>The success value.</value>
    /// <exception cref="InvalidOperationException">If the result represents a failure.</exception>
    public TSuccess AsSuccess =>
        IsError
            ? throw new InvalidOperationException("Result is not a success.")
            : _success;

    /// <summary>
    /// Gets the error value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not an error.
    /// </summary>
    /// <value>The error value.</value>
    /// <exception cref="InvalidOperationException">If the result represents a success.</exception>
    public TError AsError =>
        IsSuccess
            ? throw new InvalidOperationException("Result is not an error.")
            : _error;

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a successful operation.
    /// </summary>
    /// <param name="success">The success value.</param>
    /// <returns>A new instance of <see cref="Result{TSuccess,TError}"/> in a success state.</returns>
    public static Result<TSuccess, TError> Success(TSuccess success) => new(true, success: success);

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a failed operation.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A new instance of <see cref="Result{TSuccess,TError}"/> in an error state.</returns>
    public static Result<TSuccess, TError> Error(TError error) => new(false, error: error);

    /// <summary>
    /// Attempts to get the success value.
    /// </summary>
    /// <param name="success">When this method returns, contains the success value if the operation was successful;
    /// otherwise, the default value for <typeparamref name="TSuccess"/>.</param>
    /// <returns><c>true</c> if the operation was successful and <paramref name="success"/> contains the success value;
    /// otherwise, <c>false</c>.</returns>
    public bool TryGetSuccess([MaybeNullWhen(false)] out TSuccess success) {
        success = IsSuccess ? _success : default;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to get the error value.
    /// </summary>
    /// <param name="error">When this method returns, contains the error value if the operation failed;
    /// otherwise, the default value for <typeparamref name="TError"/>.</param>
    /// <returns><c>true</c> if the operation failed and <paramref name="error"/> contains the error value;
    /// otherwise, <c>false</c>.</returns>
    public bool TryGetError([MaybeNullWhen(false)] out TError error) {
        error = !IsSuccess ? _error : default;
        return !IsSuccess;
    }

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut>(Func<TSuccess, TOut> mapper) =>
        IsSuccess ? Result<TOut, TError>.Success(mapper(_success)) : Result<TOut, TError>.Error(_error);

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">A function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    public Result<TOut, TError> Map<TOut, TState>(Func<TSuccess, TState, TOut> mapper, TState state) =>
        IsSuccess ? Result<TOut, TError>.Success(mapper(_success, state)) : Result<TOut, TError>.Error(_error);

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut>(Func<TSuccess, Result<TOut, TError>> binder) =>
        IsSuccess ? binder(_success) : Result<TOut, TError>.Error(_error);

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    public Result<TOut, TError> Then<TOut, TState>(Func<TSuccess, TState, Result<TOut, TError>> binder, TState state) =>
        IsSuccess ? binder(_success, state) : Result<TOut, TError>.Error(_error);

    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TError, TResult> onError) =>
        IsSuccess ? onSuccess(_success) : onError(_error);

    /// <summary>
    /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The function to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
    public TResult Match<TResult, TState>(Func<TSuccess, TState, TResult> onSuccess, Func<TError, TState, TResult> onError, TState state) =>
        IsSuccess ? onSuccess(_success, state) : onError(_error, state);

    /// <summary>
    /// Executes one of the two provided actions depending on whether this result is a success or an error.
    /// This method is for side-effects and does not return a value.
    /// </summary>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value as input.</param>
    public void Switch(Action<TSuccess> onSuccess, Action<TError> onError) {
        if (IsSuccess) onSuccess(_success);
        else onError(_error);
    }

    /// <summary>
    /// Executes one of the two provided actions depending on whether this result is a success or an error, passing additional state.
    /// This method is for side-effects and does not return a value.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the actions.</typeparam>
    /// <param name="onSuccess">The action to execute if the result is a success. It takes the success value and state as input.</param>
    /// <param name="onError">The action to execute if the result is an error. It takes the error value and state as input.</param>
    /// <param name="state">The state to pass to the actions.</param>
    public void Switch<TState>(Action<TSuccess, TState> onSuccess, Action<TError, TState> onError, TState state) {
        if (IsSuccess) onSuccess(_success, state);
        else onError(_error, state);
    }

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error.
    /// </summary>
    /// <param name="fallback">A function that takes the error value and returns a fallback success value.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TSuccess GetValueOrElse(Func<TError, TSuccess> fallback) =>
        IsSuccess ? _success : fallback(_error);

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, returns a fallback value computed from the error, passing additional state.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">A function that takes the error value and state, and returns a fallback success value.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    /// <returns>The success value if <see cref="IsSuccess"/> is <c>true</c>; otherwise, the result of the <paramref name="fallback"/> function.</returns>
    public TSuccess GetValueOrElse<TState>(Func<TError, TState, TSuccess> fallback, TState state) =>
        IsSuccess ? _success : fallback(_error, state);

    /// <summary>
    /// Performs the specified action on the success value if the result is a success.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the success value.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnSuccess(Action<TSuccess> action) {
        if (IsSuccess) action(_success);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the success value if the result is a success, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnSuccess<TState>(Action<TSuccess, TState> action, TState state) {
        if (IsSuccess) action(_success, state);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <param name="action">The action to perform on the error value.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnError(Action<TError> action) {
        if (!IsSuccess) action(_error);
        return this;
    }

    /// <summary>
    /// Performs the specified action on the error value if the result is an error, passing additional state.
    /// Returns the original result, allowing for fluent chaining.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the error value, taking additional state.</param>
    /// <param name="state">The state to pass to the action.</param>
    /// <returns>The current <see cref="Result{TSuccess,TError}"/> instance.</returns>
    public Result<TSuccess, TError> OnError<TState>(Action<TError, TState> action, TState state) {
        if (!IsSuccess) action(_error, state);
        return this;
    }

    /// <summary>
    /// Implicitly converts a success value to a <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <param name="success">The success value to convert.</param>
    /// <returns>A <see cref="Result{TSuccess,TError}"/> representing success.</returns>
    public static implicit operator Result<TSuccess, TError>(TSuccess success) => Success(success);

    /// <summary>
    /// Implicitly converts an error value to a <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    /// <returns>A <see cref="Result{TSuccess,TError}"/> representing an error.</returns>
    public static implicit operator Result<TSuccess, TError>(TError error) => Error(error);

    /// <summary>
    /// Implicitly converts the result to a boolean indicating success.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns><c>true</c> if the result is a success; otherwise, <c>false</c>.</returns>
    public static implicit operator bool(Result<TSuccess, TError> result) => result.IsSuccess;

    /// <summary>
    /// Explicitly converts the result to its success value.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The success value if the result is a success.</returns>
    /// <exception cref="InvalidOperationException">If the result is not a success (i.e., it's an error).</exception>
    public static explicit operator TSuccess(Result<TSuccess, TError> result) => result.AsSuccess;

    /// <summary>
    /// Explicitly converts the result to its error value.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The error value if the result is an error.</returns>
    /// <exception cref="InvalidOperationException">If the result is not an error (i.e., it's a success).</exception>
    public static explicit operator TError(Result<TSuccess, TError> result) => result.AsError;

    /// <summary>
    /// Provides a string representation suitable for debugging purposes.
    /// </summary>
    /// <returns>A string representation of the result.</returns>
    public string ToDebugString() =>
        IsSuccess
            ? $"Success: {_success?.ToString() ?? "null"}"
            : $"Error: {_error?.ToString() ?? "null"}";
}
