using System.ComponentModel.DataAnnotations;

namespace Kurrent.Client.PersistentSubscriptions;

/// <summary>
/// Validates that a TimeSpan value is within the specified range.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TimeSpanRangeAttribute : ValidationAttribute {
    /// <summary>
    /// Gets or sets the minimum allowed value in milliseconds.
    /// </summary>
    public double MinMilliseconds { get; init; } = 0;

    /// <summary>
    /// Gets or sets the maximum allowed value in milliseconds.
    /// </summary>
    public double MaxMilliseconds { get; init; } = double.MaxValue;

    /// <summary>
    /// Determines whether the specified value is valid.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>True if the value is valid; otherwise, false.</returns>
    public override bool IsValid(object? value) {
        if (value is not TimeSpan timeSpan)
            return false;

        return timeSpan.TotalMilliseconds >= MinMilliseconds &&
               timeSpan.TotalMilliseconds <= MaxMilliseconds;
    }

    /// <summary>
    /// Formats the error message.
    /// </summary>
    /// <param name="name">The name of the property being validated.</param>
    /// <returns>The formatted error message.</returns>
    public override string FormatErrorMessage(string name) =>
        $"{name} must be greater than or equal to {TimeSpan.FromMilliseconds(MinMilliseconds)} and less than or equal to {TimeSpan.FromMilliseconds(MaxMilliseconds)}.";
}
