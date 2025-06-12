using System.Diagnostics;

namespace Kurrent.Client;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// This class provides a functional approach to error handling, avoiding exceptions for expected failures.
/// It supports direct instantiation, inheritance for domain-specific result types, and a fluent API for transformations.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToString(),nq}")]
public partial class Result<TSuccess, TError> {
    readonly TSuccess? _success;
    readonly TError?   _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess,TError}"/> class.
    /// This constructor is protected to encourage use of static factory methods <see cref="Success(TSuccess)"/> and <see cref="Error(TError)"/>
    /// or for use by derived classes.
    /// </summary>
    protected Result(bool isSuccess, TSuccess? success, TError? error) {
        if (success is null && error is null)
            throw new InvalidOperationException("Both success and error cannot be null. At least one must be provided.");

        IsSuccess = isSuccess;
        if (IsSuccess)
            _success = success;
        else
            _error  = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsError => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not a success.
    /// </summary>
    public TSuccess AsSuccess =>
        IsSuccess ? _success! : throw new InvalidOperationException("Result is not a success.");

    /// <summary>
    /// Gets the error value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not an error.
    /// </summary>
    public TError AsError =>
        IsError ? _error! : throw new InvalidOperationException("Result is not an error.");

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a successful operation.
    /// </summary>
    public static Result<TSuccess, TError> Success(TSuccess success) => new(true, success, default);

    /// <summary>
    /// Creates a new <see cref="Result{TSuccess,TError}"/> representing a failed operation.
    /// </summary>
    public static Result<TSuccess, TError> Error(TError error) => new(false, default, error);

    public override string ToString() => IsSuccess ? $"Success({AsSuccess})" : $"Failure({AsError})";

    #region . Implicit and Explicit Conversions .

    /// <summary>
    /// Implicitly converts a success value to a <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <param name="value">The success value to convert.</param>
    /// <returns>A <see cref="Result{TSuccess,TError}"/> representing success.</returns>
    public static implicit operator Result<TSuccess, TError>(TSuccess value) => Success(value);

    /// <summary>
    /// Implicitly converts an error value to a <see cref="Result{TSuccess,TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    /// <returns>A <see cref="Result{TSuccess,TError}"/> representing an error.</returns>
    public static implicit operator Result<TSuccess, TError>(TError error) => Error(error);

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

    #endregion
}
