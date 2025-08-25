using System.Diagnostics;

namespace Kurrent;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with an error.
/// This class provides a functional approach to error handling, avoiding exceptions for expected failures.
/// It supports direct instantiation, inheritance for domain-specific result types, and a fluent API for transformations.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial record struct Result<TValue, TError> : IResult<TValue, TError> where TValue : notnull where TError : notnull {
    readonly TValue? _value;
    readonly TError? _error;

    internal Result(TValue value) {
        ArgumentNullException.ThrowIfNull(value);
        Case   = ResultCase.Success;
        _value = value;
        _error = default;
    }

    internal Result(TError error) {
        ArgumentNullException.ThrowIfNull(error);
        Case   = ResultCase.Failure;
        _value = default;
        _error = error;
    }

    public ResultCase Case { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Case == ResultCase.Success;

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => Case == ResultCase.Failure;

    /// <summary>
    /// Gets the success value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not a success.
    /// </summary>
    public TValue Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Not a successful operation. No value available.");

    /// <summary>
    /// Gets the error value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not an error.
    /// </summary>
    public TError Error =>
        IsFailure ? _error! : throw new InvalidOperationException("Not a failed operation. No error available.");

    public override string ToString() => IsSuccess ? $"Success({Value})" : $"Failure({Error})";

    /// <summary>
    /// Deconstructs the instance into its component parts.
    /// </summary>
    /// <param name="success">The success component of the result.</param>
    /// <param name="error">The error component of the result.</param>
    public void Deconstruct(out TValue? success, out TError? error) {
        success = _value;
        error   = _error;
    }

    #region . implicit and explicit conversions .

    public static implicit operator bool(Result<TValue, TError> result) => result.IsSuccess;

    public static implicit operator ResultCase(Result<TValue, TError> result) => result.Case;

    /// <summary>
    /// Implicitly converts a success value to a <see cref="Result{TValue,TError}"/>.
    /// </summary>
    /// <param name="value">The success value to convert.</param>
    /// <returns>A <see cref="Result{TValue,TError}"/> representing success.</returns>
    public static implicit operator Result<TValue, TError>(TValue value) => new(value);

    /// <summary>
    /// Implicitly converts an error value to a <see cref="Result{TValue,TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    /// <returns>A <see cref="Result{TValue,TError}"/> representing an error.</returns>
    public static implicit operator Result<TValue, TError>(TError error) => new(error);

    /// <summary>
    /// Explicitly converts the result to its success value.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The success value if the result is a success.</returns>
    /// <exception cref="InvalidOperationException">If the result is not a success (i.e., it's an error).</exception>
    public static explicit operator TValue(Result<TValue, TError> result) => result.Value;

    /// <summary>
    /// Explicitly converts the result to its error value.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <returns>The error value if the result is an error.</returns>
    /// <exception cref="InvalidOperationException">If the result is not an error (i.e., it's a success).</exception>
    public static explicit operator TError(Result<TValue, TError> result) => result.Error;

    #endregion
}

public static partial class Result {
    /// <summary>
    /// Creates a new <see cref="Result{TValue,TError}"/> representing a successful operation with the specified value.
    /// </summary>
    public static Result<TValue, TError> Success<TValue, TError>(TValue value) => new(value);

    /// <summary>
    /// Creates a new <see cref="Result{TValue,TError}"/> representing a failed operation with the specified error.
    /// </summary>
    public static Result<TValue, TError> Failure<TValue, TError>(TError error) => new(error);

    public static ValueTask<Result<TValue, TError>> SuccessTask<TValue, TError>(TValue value) =>
        ValueTask.FromResult(Success<TValue, TError>(value));

    public static ValueTask<Result<TValue, TError>> FailureTask<TValue, TError>(TError error) =>
        ValueTask.FromResult(Failure<TValue, TError>(error));
}

public enum ResultCase {
    Success, Failure
}
