using System.Diagnostics;

namespace Kurrent;

/// <summary>
/// Represents a value that can be one of two possible types: <typeparamref name="TFirst"/>
/// or <typeparamref name="TSecond"/>.
/// This is a generic discriminated union for two cases, similar to an Either type.
/// </summary>
/// <typeparam name="TFirst">The type of the first possible value.</typeparam>
/// <typeparam name="TSecond">The type of the second possible value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToString(),nq}")]
public class Variant<TFirst, TSecond> : IEquatable<Variant<TFirst, TSecond>> {
    readonly TFirst?  _firstValue;
    readonly TSecond? _secondValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Variant{TFirst,TSecond}"/> class.
    /// This constructor is protected to encourage use of static factory methods.
    /// </summary>
    protected Variant(bool isSecond, TFirst? first, TSecond? second) {
        IsSecond = isSecond;
        if (IsSecond) {
            if (second is null && default(TSecond) is not null)
                throw new ArgumentNullException(nameof(second), "Second value cannot be null if it's the chosen case and TSecond is non-nullable.");
            _secondValue = second;
        }
        else {
            if (first is null && default(TFirst) is not null)
                throw new ArgumentNullException(nameof(first), "First value cannot be null if it's the chosen case and TFirst is non-nullable.");
            _firstValue = first;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TSecond"/>.
    /// </summary>
    public bool IsSecond { get; }

    /// <summary>
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TFirst"/>.
    /// </summary>
    public bool IsFirst => !IsSecond;

    /// <summary>
    /// Gets the value of type <typeparamref name="TFirst"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TFirst"/> value.
    /// </summary>
    public TFirst AsFirst =>
        IsFirst ? _firstValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TFirst).Name}' value.");

    /// <summary>
    /// Gets the value of type <typeparamref name="TSecond"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TSecond"/> value.
    /// </summary>
    public TSecond AsSecond =>
        IsSecond ? _secondValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TSecond).Name}' value.");

    public override string ToString() => IsFirst ? $"First({_firstValue})" : $"Second({_secondValue})";

    /// <summary>
    /// Executes one of the provided functions based on the type of value this instance holds.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the functions.</typeparam>
    /// <param name="onFirst">The function to execute if this instance holds a <typeparamref name="TFirst"/> value.</param>
    /// <param name="onSecond">The function to execute if this instance holds a <typeparamref name="TSecond"/> value.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(
        Func<TFirst, TResult> onFirst,
        Func<TSecond, TResult> onSecond) {
        ArgumentNullException.ThrowIfNull(onFirst);
        ArgumentNullException.ThrowIfNull(onSecond);

        return IsFirst ? onFirst(AsFirst) : onSecond(AsSecond);
    }

    #region . equality members .

    public bool Equals(Variant<TFirst, TSecond>? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return IsSecond == other.IsSecond
            && EqualityComparer<TFirst?>.Default.Equals(_firstValue, other._firstValue)
            && EqualityComparer<TSecond?>.Default.Equals(_secondValue, other._secondValue);
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is Variant<TFirst, TSecond> other && Equals(other);
    }

    public override int GetHashCode() => HashCode.Combine(IsSecond, _firstValue, _secondValue);

    public static bool operator ==(Variant<TFirst, TSecond>? left, Variant<TFirst, TSecond>? right) =>
        Equals(left, right);

    public static bool operator !=(Variant<TFirst, TSecond>? left, Variant<TFirst, TSecond>? right) =>
        !Equals(left, right);

    #endregion

    #region . static factory methods .

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond}"/> holding a <typeparamref name="TFirst"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond> First(TFirst value) => new(false, value, default);

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond}"/> holding a <typeparamref name="TSecond"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond> Second(TSecond value) => new(true, default, value);

    #endregion

    #region . implicit and explicit conversions .

    public static implicit operator Variant<TFirst, TSecond>(TFirst value)  => First(value);
    public static implicit operator Variant<TFirst, TSecond>(TSecond value) => Second(value);

    public static explicit operator TFirst(Variant<TFirst, TSecond> variant)  => variant.AsFirst;
    public static explicit operator TSecond(Variant<TFirst, TSecond> variant) => variant.AsSecond;

    #endregion
}
