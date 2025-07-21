namespace Kurrent;

/// <summary>
/// Non-generic base interface for all result types, providing common properties for success/failure state checking.
/// </summary>
[PublicAPI]
public interface IResult {
    /// <summary>
    /// Gets the case of the result, indicating whether it represents a success or failure.
    /// </summary>
    ResultCase Case { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    bool IsFailure { get; }
}