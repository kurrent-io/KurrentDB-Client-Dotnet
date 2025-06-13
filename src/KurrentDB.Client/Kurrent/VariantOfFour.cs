using System.Diagnostics;

namespace Kurrent;

/// <summary>
/// Represents a value that can be one of four possible types: <typeparamref name="TFirst"/>,
/// <typeparamref name="TSecond"/>, <typeparamref name="TThird"/>, or <typeparamref name="TFourth"/>.
/// This is a generic discriminated union for four cases.
/// </summary>
/// <typeparam name="TFirst">The type of the first possible value.</typeparam>
/// <typeparam name="TSecond">The type of the second possible value.</typeparam>
/// <typeparam name="TThird">The type of the third possible value.</typeparam>
/// <typeparam name="TFourth">The type of the fourth possible value.</typeparam>
[PublicAPI]
[DebuggerDisplay("{ToString(),nq}")]
public class Variant<TFirst, TSecond, TThird, TFourth> : IEquatable<Variant<TFirst, TSecond, TThird, TFourth>> {
    readonly Case _activeCase;

    readonly TFirst?  _firstValue;
    readonly TFourth? _fourthValue;
    readonly TSecond? _secondValue;
    readonly TThird?  _thirdValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Variant{TFirst,TSecond,TThird,TFourth}"/> class.
    /// This constructor is protected to encourage use of static factory methods.
    /// </summary>
    protected Variant(
        Case activeCase, TFirst? first, TSecond? second, TThird? third,
        TFourth? fourth
    ) {
        _activeCase = activeCase;
        switch (_activeCase) {
            case Case.First:
                if (first is null && default(TFirst) is not null)
                    throw new ArgumentNullException(nameof(first), "First value cannot be null if it's the chosen case and TFirst is non-nullable.");

                _firstValue = first;
                break;

            case Case.Second:
                if (second is null && default(TSecond) is not null)
                    throw new ArgumentNullException(nameof(second), "Second value cannot be null if it's the chosen case and TSecond is non-nullable.");

                _secondValue = second;
                break;

            case Case.Third:
                if (third is null && default(TThird) is not null)
                    throw new ArgumentNullException(nameof(third), "Third value cannot be null if it's the chosen case and TThird is non-nullable.");

                _thirdValue = third;
                break;

            case Case.Fourth:
                if (fourth is null && default(TFourth) is not null)
                    throw new ArgumentNullException(nameof(fourth), "Fourth value cannot be null if it's the chosen case and TFourth is non-nullable.");

                _fourthValue = fourth;
                break;

            default:
                throw new InvalidOperationException("Variant must be initialized with a valid case.");
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
    /// Gets a value indicating whether this instance holds a value of type <typeparamref name="TFourth"/>.
    /// </summary>
    public bool IsFourth => _activeCase == Case.Fourth;

    /// <summary>
    /// Gets the value of type <typeparamref name="TFirst"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TFirst"/> value.
    /// </summary>
    public TFirst AsFirst => IsFirst ? _firstValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TFirst).Name}' value.");

    /// <summary>
    /// Gets the value of type <typeparamref name="TSecond"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TSecond"/> value.
    /// </summary>
    public TSecond AsSecond => IsSecond ? _secondValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TSecond).Name}' value.");

    /// <summary>
    /// Gets the value of type <typeparamref name="TThird"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TThird"/> value.
    /// </summary>
    public TThird AsThird => IsThird ? _thirdValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TThird).Name}' value.");

    /// <summary>
    /// Gets the value of type <typeparamref name="TFourth"/>.
    /// Throws an <see cref="InvalidOperationException"/> if this instance does not hold a <typeparamref name="TFourth"/> value.
    /// </summary>
    public TFourth AsFourth => IsFourth ? _fourthValue! : throw new InvalidOperationException($"Instance does not hold a '{typeof(TFourth).Name}' value.");

    public override string ToString() =>
        _activeCase switch {
            Case.First  => $"First({_firstValue})",
            Case.Second => $"Second({_secondValue})",
            Case.Third  => $"Third({_thirdValue})",
            Case.Fourth => $"Fourth({_fourthValue})",
            _           => "Variant(Uninitialized)"
        };

    /// <summary>
    /// Executes one of the provided functions based on the type of value this instance holds.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the functions.</typeparam>
    /// <param name="onFirst">The function to execute if this instance holds a <typeparamref name="TFirst"/> value.</param>
    /// <param name="onSecond">The function to execute if this instance holds a <typeparamref name="TSecond"/> value.</param>
    /// <param name="onThird">The function to execute if this instance holds a <typeparamref name="TThird"/> value.</param>
    /// <param name="onFourth">The function to execute if this instance holds a <typeparamref name="TFourth"/> value.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">If the variant is in an unexpected state.</exception>
    public TResult Match<TResult>(
        Func<TFirst, TResult> onFirst,
        Func<TSecond, TResult> onSecond,
        Func<TThird, TResult> onThird,
        Func<TFourth, TResult> onFourth
    ) {
        ArgumentNullException.ThrowIfNull(onFirst);
        ArgumentNullException.ThrowIfNull(onSecond);
        ArgumentNullException.ThrowIfNull(onThird);
        ArgumentNullException.ThrowIfNull(onFourth);

        return _activeCase switch {
            Case.First  => onFirst(AsFirst),
            Case.Second => onSecond(AsSecond),
            Case.Third  => onThird(AsThird),
            Case.Fourth => onFourth(AsFourth),
            _           => throw new InvalidOperationException("Variant is in an invalid state.")
        };
    }

    protected enum Case {
        None,
        First,
        Second,
        Third,
        Fourth
    }

    #region . equality members .

    public bool Equals(Variant<TFirst, TSecond, TThird, TFourth>? other) {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_activeCase != other._activeCase) return false;

        return _activeCase switch {
            Case.First  => EqualityComparer<TFirst?>.Default.Equals(_firstValue, other._firstValue),
            Case.Second => EqualityComparer<TSecond?>.Default.Equals(_secondValue, other._secondValue),
            Case.Third  => EqualityComparer<TThird?>.Default.Equals(_thirdValue, other._thirdValue),
            Case.Fourth => EqualityComparer<TFourth?>.Default.Equals(_fourthValue, other._fourthValue),
            _           => true
        };
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;

        return obj is Variant<TFirst, TSecond, TThird, TFourth> other && Equals(other);
    }

    public override int GetHashCode() =>
        _activeCase switch {
            Case.First  => HashCode.Combine(_activeCase, _firstValue),
            Case.Second => HashCode.Combine(_activeCase, _secondValue),
            Case.Third  => HashCode.Combine(_activeCase, _thirdValue),
            Case.Fourth => HashCode.Combine(_activeCase, _fourthValue),
            _           => HashCode.Combine(_activeCase)
        };

    public static bool operator ==(Variant<TFirst, TSecond, TThird, TFourth>? left, Variant<TFirst, TSecond, TThird, TFourth>? right) => Equals(left, right);

    public static bool operator !=(Variant<TFirst, TSecond, TThird, TFourth>? left, Variant<TFirst, TSecond, TThird, TFourth>? right) => !Equals(left, right);

    #endregion

    #region . static factory methods .

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird,TFourth}"/> holding a <typeparamref name="TFirst"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird, TFourth> First(TFirst value) =>
        new(
            Case.First, value, default,
            default, default
        );

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird,TFourth}"/> holding a <typeparamref name="TSecond"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird, TFourth> Second(TSecond value) =>
        new(
            Case.Second, default, value,
            default, default
        );

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird,TFourth}"/> holding a <typeparamref name="TThird"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird, TFourth> Third(TThird value) =>
        new(
            Case.Third, default, default,
            value, default
        );

    /// <summary>
    /// Creates a new <see cref="Variant{TFirst,TSecond,TThird,TFourth}"/> holding a <typeparamref name="TFourth"/> value.
    /// </summary>
    public static Variant<TFirst, TSecond, TThird, TFourth> Fourth(TFourth value) =>
        new(
            Case.Fourth, default, default,
            default, value
        );

    #endregion

    #region . implicit and explicit conversions .

    public static implicit operator Variant<TFirst, TSecond, TThird, TFourth>(TFirst value)  => First(value);
    public static implicit operator Variant<TFirst, TSecond, TThird, TFourth>(TSecond value) => Second(value);
    public static implicit operator Variant<TFirst, TSecond, TThird, TFourth>(TThird value)  => Third(value);
    public static implicit operator Variant<TFirst, TSecond, TThird, TFourth>(TFourth value) => Fourth(value);

    public static explicit operator TFirst(Variant<TFirst, TSecond, TThird, TFourth> variant)  => variant.AsFirst;
    public static explicit operator TSecond(Variant<TFirst, TSecond, TThird, TFourth> variant) => variant.AsSecond;
    public static explicit operator TThird(Variant<TFirst, TSecond, TThird, TFourth> variant)  => variant.AsThird;
    public static explicit operator TFourth(Variant<TFirst, TSecond, TThird, TFourth> variant) => variant.AsFourth;

    #endregion
}
