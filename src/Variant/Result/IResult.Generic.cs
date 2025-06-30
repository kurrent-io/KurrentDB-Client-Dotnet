namespace Kurrent;

/// <summary>
/// Generic interface for result types that can either succeed with a value or fail with an error.
/// Provides type-safe access to success values and error values while maintaining the common result contract.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
[PublicAPI]
public interface IResult<out TValue, out TError> : IResult where TValue : notnull where TError : notnull {
    /// <summary>
    /// Gets the success value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not a success.
    /// </summary>
    TValue Value { get; }

    /// <summary>
    /// Gets the error value.
    /// Throws an <see cref="InvalidOperationException"/> if the result is not an error.
    /// </summary>
    TError Error { get; }
}