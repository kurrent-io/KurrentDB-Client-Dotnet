using System.Diagnostics;

namespace Kurrent;

/// <summary>
/// Represents a value that can be one of three possible types: <typeparamref name="TFirst"/>,
/// <typeparamref name="TSecond"/>, or <typeparamref name="TThird"/>.
/// This is a generic discriminated union for three cases.
/// </summary>
/// <typeparam name="TFirst">The type of the first possible value.</typeparam>
/// <typeparam name="TSecond">The type of the second possible value.</typeparam>
/// <typeparam name="TThird">The type of the third possible value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToString(),nq}")]
public class Variant<TFirst, TSecond, TThird> : IEquatable<Variant<TFirst, TSecond, TThird>> {
    readonly Case _activeCase;

    readonly TFirst?  _firstValue;
    readonly TSecond? _secondValue;
    readonly TThird?  _thirdValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Variant{TFirst,TSecond,TThird}"/> class.
    /// This constructor is protected to encourage use of static factory methods.
    /// </summary>
    protected Variant(Case activeCase, TFirst? first, TSecond? second, TThird? third) {
        _activeCase = activeCase;

        if (_activeCase == Case.First) {
            if (first is null && default(TFirst) is not null)
                throw new ArgumentNullException(nameof(first), "First value cannot be null if it's the chosen case and TFirst is non-nullable.");
            _firstValue = first;
        }
        else if (_activeCase == Case.Second) {
            if (second is null && default(TSecond) is not null)
                throw new ArgumentNullException(nameof(second), "Second value cannot be null if it's the chosen case and TSecond is non-nullable.");
            _secondValue = second;
        }
        else if (_activeCase == Case.Third) {
            if (third is null && default(TThird) is not null)
                throw new ArgumentNullException(nameof(third), "Third value cannot be null if it's the chosen case and TThird is non-nullable.");
            _thirdValue = third;
        }
        else {
            throw new InvalidOperationException("Variant must be initialized with a valid case (First, Second, or Third).");
        }
    }

    /// <summary>
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TFirst"/>.
    /// </summary>
    public bool IsFirst => _activeCase == Case.First;

    /// <summary>
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TSecond"/>.
    /// </summary>
    public bool IsSecond => _activeCase == Case.Second;

    /// <summary>
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TThird"/>.
    /// </summary>
    public bool IsThird => _activeCase == Case.Third;

    /// <summary>
    /// Gets the value of type <typeparamref name="TFirst"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TFirst"/> value.
    /// </summary>
    public TFirst AsFirst {
        get {
            if (!IsFirst) throw new InvalidOperationException($"Instance does not hold a '{typeof(TFirst).Name}' value.");
            return _firstValue!;
        }
    }

    /// <summary>
    /// Gets the value of type <typeparamref name="TSecond"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TSecond"/> value.
    /// </summary>
    public TSecond AsSecond {
        get {
            if (!IsSecond) throw new InvalidOperationException($"Instance does not hold a '{typeof(TSecond).Name}' value.");
            return _secondValue!;
        }
    }

    /// <summary>
    /// Gets the value of type <typeparamref name="TThird"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TThird"/> value.
    /// </summary>
    public TThird AsThird {
        get {
            if (!IsThird) throw new InvalidOperationException($"Instance does not hold a '{typeof(TThird).Name}' value.");
            return _thirdValue!;
        }
    }

    public override string ToString() =>
        _activeCase switch {
            Case.First  => $"First({_firstValue})",
            Case.Second => $"Second({_secondValue})",
            Case.Third  => $"Third({_thirdValue})",
            _           => "Variant(Invalid)" // Should not happen with proper construction
        };

    /// <summary>
    /// Executes one of the provided functions based on the type of value this instance holds.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the functions.</typeparam>
    /// <param name="onFirst">The function to execute if this instance holds a <typeparamref name="TFirst"/> value.</param>
    /// <param name="onSecond">The function to execute if this instance holds a <typeparamref name="TSecond"/> value.</param>
    /// <param name="onThird">The function to execute if this instance holds a <typeparamref name="TThird"/> value.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">If the variant is in an unexpected state.</exception>
    public TResult Match<TResult>(
        Func<TFirst, TResult> onFirst,
        Func<TSecond, TResult> onSecond,
        Func<TThird, TResult> onThird
    ) {
        ArgumentNullException.ThrowIfNull(onFirst);
        ArgumentNullException.ThrowIfNull(onSecond);
        ArgumentNullException.ThrowIfNull(onThird);

        if (IsFirst) return onFirst(AsFirst);
        if (IsSecond) return onSecond(AsSecond);
        if (IsThird) return onThird(AsThird);
        
        throw new InvalidOperationException("Variant is in an invalid or unhandled state."); // Should be unreachable
    }

    protected enum Case {
        // None, // 'None' is removed to ensure _activeCase always represents a valid choice for T1, T2, or T3.
        First,
        Second,
        Third
    }

    #region . equality members .

    public bool Equals(Variant<TFirst, TSecond, TThird>? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_activeCase != other._activeCase) return false;

        if (IsFirst) return EqualityComparer<TFirst?>.Default.Equals(_firstValue, other._firstValue);
        if (IsSecond) return EqualityComparer<TSecond?>.Default.Equals(_secondValue, other._secondValue);
        if (IsThird) return EqualityComparer<TThird?>.Default.Equals(_thirdValue, other._thirdValue);
        
        return false; // Should be unreachable if _activeCase matched and is one of the valid cases.
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;

        return obj is Variant<TFirst, TSecond, TThird> other && Equals(other);
    }

    public override int GetHashCode() {
        if (IsFirst) return HashCode.Combine(_activeCase, _firstValue);
        if (IsSecond) return HashCode.Combine(_activeCase, _secondValue);
        if (IsThird) return HashCode.Combine(_activeCase, _thirdValue);

        return HashCode.Combine(_activeCase); // Should be unreachable
    }
    // A more general approach, though slightly less efficient if only one field is active:
    // return HashCode.Combine(_activeCase, _firstValue, _secondValue, _thirdValue);

    public static bool operator ==(Variant<TFirst, TSecond, TThird>? left, Variant<TFirst, TSecond, TThird>? right) => Equals(left, right);

    public static bool operator !=(Variant<TFirst, TSecond, TThird>? left, Variant<TFirst, TSecond, TThird>? right) => !Equals(left, right);

    #endregion

    #region . static factory methods .

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird}"/> holding a <typeparamref name="TFirst"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird> First(TFirst value) =>
        new(
            Case.First, value, default,
            default
        );

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird}"/> holding a <typeparamref name="TSecond"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird> Second(TSecond value) =>
        new(
            Case.Second, default, value,
            default
        );

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird}"/> holding a <typeparamref name="TThird"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird> Third(TThird value) =>
        new(
            Case.Third, default, default,
            value
        );

    #endregion

    #region . implicit and explicit conversions .

    public static implicit operator Variant<TFirst, TSecond, TThird>(TFirst value)  => First(value);
    public static implicit operator Variant<TFirst, TSecond, TThird>(TSecond value) => Second(value);
    public static implicit operator Variant<TFirst, TSecond, TThird>(TThird value)  => Third(value);

    public static explicit operator TFirst(Variant<TFirst, TSecond, TThird> variant)  => variant.AsFirst;
    public static explicit operator TSecond(Variant<TFirst, TSecond, TThird> variant) => variant.AsSecond;
    public static explicit operator TThird(Variant<TFirst, TSecond, TThird> variant)  => variant.AsThird;

    #endregion
}
