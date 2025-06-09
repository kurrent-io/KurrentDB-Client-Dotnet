using System.Diagnostics.CodeAnalysis;
using OneOf;
using OneOf.Types;

namespace KurrentDB.Client;

/// <summary>
/// Represents a discriminated union of an error or success value,
/// providing a functional approach to error handling.
/// </summary>
/// <typeparam name="TError">The type of the error value.</typeparam>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
[PublicAPI]
public class Result<TError, TSuccess> : OneOfBase<Error<TError>, Success<TSuccess>> {
    /// <summary>
    /// Creates a new Result from a OneOf discriminated union.
    /// </summary>
    /// <param name="_">The discriminated union value.</param>
    protected Result(OneOf<Error<TError>, Success<TSuccess>> _) : base(_) { }

    /// <summary>
    /// Protected constructor for derived classes that need to set up the internal state differently.
    /// </summary>
    /// <param name="value">The value (either success or error).</param>
    /// <param name="isSuccess">Whether this is a success result.</param>
    protected Result(object value, bool isSuccess) : base(
        isSuccess
            ? OneOf<Error<TError>, Success<TSuccess>>.FromT1(new Success<TSuccess>((TSuccess)value))
            : OneOf<Error<TError>, Success<TSuccess>>.FromT0(new Error<TError>((TError)value))
    ) { }

    // Implicit Conversions
    /// <summary>
    /// Implicitly converts an Error to a Result.
    /// </summary>
    public static implicit operator Result<TError, TSuccess>(Error<TError> _) => new(_);

    /// <summary>
    /// Implicitly converts a Success to a Result.
    /// </summary>
    public static implicit operator Result<TError, TSuccess>(Success<TSuccess> _) => new(_);

    /// <summary>
    /// Implicitly converts a success value to a Result.
    /// </summary>
    public static implicit operator Result<TError, TSuccess>(TSuccess value) => new Success<TSuccess>(value);

    /// <summary>
    /// Implicitly converts an error value to a Result.
    /// </summary>
    public static implicit operator Result<TError, TSuccess>(TError value) => new Error<TError>(value);

    // Factory Methods
    /// <summary>
    /// Creates an error result with the specified error value.
    /// </summary>
    public static Result<TError, TSuccess> Error(TError value) => new Error<TError>(value);

    /// <summary>
    /// Creates a success result with the specified success value.
    /// </summary>
    public static Result<TError, TSuccess> Success(TSuccess value) => new Success<TSuccess>(value);

    /// <summary>
    /// Creates a result from a function that might throw an exception.
    /// </summary>
    /// <param name="func">A function that returns a value or throws an exception.</param>
    /// <param name="exceptionHandler">A function that transforms an exception into an error value.</param>
    public static Result<TError, TSuccess> Try(Func<TSuccess> func, Func<Exception, TError> exceptionHandler) {
        try {
            return Success(func());
        }
        catch (Exception ex) {
            return Error(exceptionHandler(ex));
        }
    }

    /// <summary>
    /// Creates a result from a function that might throw an exception, passing state to the functions.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="func">A function that takes state and returns a value or throws an exception.</param>
    /// <param name="exceptionHandler">A function that takes state and an exception, and transforms it into an error value.</param>
    /// <param name="state">The state to pass to the functions.</param>
    public static Result<TError, TSuccess> Try<TState>(Func<TState, TSuccess> func, Func<TState, Exception, TError> exceptionHandler, TState state) {
        try {
            return Success(func(state));
        }
        catch (Exception ex) {
            return Error(exceptionHandler(state, ex));
        }
    }

    /// <summary>
    /// Creates a result from a nullable reference type.
    /// </summary>
    /// <param name="value">The value which might be null.</param>
    /// <param name="errorIfNull">The error value to use if the value is null.</param>
    public static Result<TError, T> FromNullable<T>(T? value, TError errorIfNull) where T : class =>
        value is not null
            ? Result<TError, T>.Success(value)
            : Result<TError, T>.Error(errorIfNull);

    /// <summary>
    /// Creates a result from a nullable value type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="errorIfNull">The error value to use if the value is null.</param>
    public static Result<TError, T> FromNullableStruct<T>(T? value, TError errorIfNull) where T : struct =>
        value.HasValue
            ? Result<TError, T>.Success(value.Value)
            : Result<TError, T>.Error(errorIfNull);

    // Properties
    /// <summary>
    /// Checks if the result represents an error.
    /// </summary>
    public bool IsError => IsT0;

    /// <summary>
    /// Checks if the result represents a success.
    /// </summary>
    public bool IsSuccess => IsT1;

    // Public Methods
    /// <summary>
    /// Gets the error value if this result represents an error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
    public TError ErrorValue() =>
        !IsError
            ? throw new InvalidOperationException("Cannot access error value on a success result")
            : AsT0.Value;

    /// <summary>
    /// Gets the success value if this result represents a success.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is an error.</exception>
    public TSuccess SuccessValue() =>
        !IsSuccess
            ? throw new InvalidOperationException("Cannot access success value on an error result")
            : AsT1.Value;

    /// <summary>
    /// Attempts to get the success value from this result.
    /// </summary>
    /// <param name="value">The success value if present, default otherwise.</param>
    public bool TryGetSuccess([MaybeNullWhen(false)] out TSuccess value) {
        value = IsSuccess ? AsT1.Value : default;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to get the error value from this result.
    /// </summary>
    /// <param name="value">The error value if present, default otherwise.</param>
    public bool TryGetError([MaybeNullWhen(false)] out TError value) {
        value = IsError ? AsT0.Value : default;
        return IsError;
    }

    /// <summary>
    /// Transforms a success result using the specified binding function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output success value.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a new result.</param>
    public Result<TError, TOut> Then<TOut>(Func<TSuccess, Result<TError, TOut>> binder) =>
        Match<Result<TError, TOut>>(
            error   => Result<TError, TOut>.Error(error.Value),
            success => binder(success.Value)
        );

    /// <summary>
    /// Transforms a success result using the specified binding function, passing state to the function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="binder">A function that takes the success value and state, and returns a new result.</param>
    /// <param name="state">The state to pass to the function.</param>
    public Result<TError, TOut> Then<TOut, TState>(Func<TSuccess, TState, Result<TError, TOut>> binder, TState state) =>
        Match<Result<TError, TOut>>(
            error   => Result<TError, TOut>.Error(error.Value),
            success => binder(success.Value, state)
        );

    /// <summary>
    /// Transforms an error result using the specified binding function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output error value.</typeparam>
    /// <param name="binder">A function that takes the error value and returns a new result.</param>
    public Result<TOut, TSuccess> OrElse<TOut>(Func<TError, Result<TOut, TSuccess>> binder) =>
        Match<Result<TOut, TSuccess>>(
            error   => binder(error.Value),
            success => Result<TOut, TSuccess>.Success(success.Value)
        );

    /// <summary>
    /// Transforms an error result using the specified binding function, passing state to the function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="binder">A function that takes the error value and state, and returns a new result.</param>
    /// <param name="state">The state to pass to the function.</param>
    public Result<TOut, TSuccess> OrElse<TOut, TState>(Func<TError, TState, Result<TOut, TSuccess>> binder, TState state) =>
        Match<Result<TOut, TSuccess>>(
            error   => binder(error.Value, state),
            success => Result<TOut, TSuccess>.Success(success.Value)
        );

    /// <summary>
    /// Maps the success value using the specified mapping function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output success value.</typeparam>
    /// <param name="mapSuccess">A function that transforms the success value.</param>
    public Result<TError, TOut> Map<TOut>(Func<TSuccess, TOut> mapSuccess) =>
        Match<Result<TError, TOut>>(
            error   => Result<TError, TOut>.Error(error.Value),
            success => Result<TError, TOut>.Success(mapSuccess(success.Value))
        );

    /// <summary>
    /// Maps the success value using the specified mapping function, passing state to the function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="mapSuccess">A function that transforms the success value, taking state.</param>
    /// <param name="state">The state to pass to the function.</param>
    public Result<TError, TOut> Map<TOut, TState>(Func<TSuccess, TState, TOut> mapSuccess, TState state) =>
        Match<Result<TError, TOut>>(
            error   => Result<TError, TOut>.Error(error.Value),
            success => Result<TError, TOut>.Success(mapSuccess(success.Value, state))
        );

    /// <summary>
    /// Maps the error value using the specified mapping function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output error value.</typeparam>
    /// <param name="mapError">A function that transforms the error value.</param>
    public Result<TOut, TSuccess> MapError<TOut>(Func<TError, TOut> mapError) =>
        Match<Result<TOut, TSuccess>>(
            error   => Result<TOut, TSuccess>.Error(mapError(error.Value)),
            success => Result<TOut, TSuccess>.Success(success.Value)
        );

    /// <summary>
    /// Maps the error value using the specified mapping function, passing state to the function.
    /// </summary>
    /// <typeparam name="TOut">The type of the output error value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the function.</typeparam>
    /// <param name="mapError">A function that transforms the error value, taking state.</param>
    /// <param name="state">The state to pass to the function.</param>
    public Result<TOut, TSuccess> MapError<TOut, TState>(Func<TError, TState, TOut> mapError, TState state) =>
        Match<Result<TOut, TSuccess>>(
            error   => Result<TOut, TSuccess>.Error(mapError(error.Value, state)),
            success => Result<TOut, TSuccess>.Success(success.Value)
        );

    /// <summary>
    /// Performs an action on the success value if this result is a success.
    /// </summary>
    /// <param name="action">The action to perform on the success value.</param>
    public Result<TError, TSuccess> OnSuccess(Action<TSuccess> action) {
        if (IsSuccess)
            action(SuccessValue());

        return this;
    }

    /// <summary>
    /// Performs an action on the success value if this result is a success, passing state to the action.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the success value, taking state.</param>
    /// <param name="state">The state to pass to the action.</param>
    public Result<TError, TSuccess> OnSuccess<TState>(Action<TSuccess, TState> action, TState state) {
        if (IsSuccess)
            action(SuccessValue(), state);

        return this;
    }

    /// <summary>
    /// Performs an action on the error value if this result is an error.
    /// </summary>
    /// <param name="action">The action to perform on the error value.</param>
    public Result<TError, TSuccess> OnError(Action<TError> action) {
        if (IsError)
            action(ErrorValue());

        return this;
    }

    /// <summary>
    /// Performs an action on the error value if this result is an error, passing state to the action.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the action.</typeparam>
    /// <param name="action">The action to perform on the error value, taking state.</param>
    /// <param name="state">The state to pass to the action.</param>
    public Result<TError, TSuccess> OnError<TState>(Action<TError, TState> action, TState state) {
        if (IsError)
            action(ErrorValue(), state);

        return this;
    }

    /// <summary>
    /// Returns the success value, or falls back to a value derived from the error.
    /// </summary>
    /// <param name="fallback">A function to transform the error into a success value.</param>
    public TSuccess GetValueOrElse(Func<TError, TSuccess> fallback) =>
        Match<TSuccess>(
            error   => fallback(error.Value),
            success => success.Value
        );

    /// <summary>
    /// Returns the success value, or falls back to a value derived from the error, passing state to the fallback function.
    /// </summary>
    /// <typeparam name="TState">The type of the state to pass to the fallback function.</typeparam>
    /// <param name="fallback">A function to transform the error into a success value, taking state.</param>
    /// <param name="state">The state to pass to the fallback function.</param>
    public TSuccess GetValueOrElse<TState>(Func<TError, TState, TSuccess> fallback, TState state) =>
        Match<TSuccess>(
            error   => fallback(error.Value, state),
            success => success.Value
        );

    /// <summary>
    /// Transforms this result into a value of a different type based on whether it's an error or success.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <param name="caseError">A function to transform the error value.</param>
    /// <param name="caseSuccess">A function to transform the success value.</param>
    public TOut Fold<TOut>(Func<TError, TOut> caseError, Func<TSuccess, TOut> caseSuccess) =>
        Match<TOut>(
            error   => caseError(error.Value),
            success => caseSuccess(success.Value)
        );

    /// <summary>
    /// Transforms this result into a value of a different type based on whether it's an error or success, passing state to the functions.
    /// </summary>
    /// <typeparam name="TOut">The type of the output value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="caseError">A function to transform the error value, taking state.</param>
    /// <param name="caseSuccess">A function to transform the success value, taking state.</param>
    /// <param name="state">The state to pass to the functions.</param>
    public TOut Fold<TOut, TState>(Func<TError, TState, TOut> caseError, Func<TSuccess, TState, TOut> caseSuccess, TState state) =>
        Match<TOut>(
            error   => caseError(error.Value, state),
            success => caseSuccess(success.Value, state)
        );

    /// <summary>
    /// Combines this result with another using the specified combining function.
    /// </summary>
    /// <typeparam name="TSuccessOut">The output success type.</typeparam>
    /// <typeparam name="TSuccessOther">The success type of the other result.</typeparam>
    /// <param name="other">The result to combine with this one.</param>
    /// <param name="combine">A function to combine the success values.</param>
    public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessOther>(
        Result<TError, TSuccessOther> other,
        Func<TSuccess, TSuccessOther, TSuccessOut> combine
    ) {
        if (!IsSuccess)
            return ErrorValue();

        if (!other.IsSuccess)
            return other.ErrorValue();

        return combine(SuccessValue(), other.SuccessValue());
    }

    /// <summary>
    /// Combines this result with two others using the specified combining function.
    /// </summary>
    /// <param name="firstOther">The first result to combine with this one.</param>
    /// <param name="secondOther">The second result to combine with this one.</param>
    /// <param name="combine">A function to combine the success values.</param>
    public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther>(
        Result<TError, TSuccessFirstOther> firstOther,
        Result<TError, TSuccessSecondOther> secondOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessOut> combine
    ) {
        if (!IsSuccess)
            return ErrorValue();

        if (!firstOther.IsSuccess)
            return firstOther.ErrorValue();

        if (!secondOther.IsSuccess)
            return secondOther.ErrorValue();

        return combine(SuccessValue(), firstOther.SuccessValue(), secondOther.SuccessValue());
    }

    /// <summary>
    /// Combines this result with three others using the specified combining function.
    /// </summary>
    /// <param name="firstOther">The first result to combine with this one.</param>
    /// <param name="secondOther">The second result to combine with this one.</param>
    /// <param name="thirdOther">The third result to combine with this one.</param>
    /// <param name="combine">A function to combine the success values.</param>
    public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther>(
        Result<TError, TSuccessFirstOther> firstOther,
        Result<TError, TSuccessSecondOther> secondOther,
        Result<TError, TSuccessThirdOther> thirdOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessOut> combine
    ) {
        if (!IsSuccess)
            return ErrorValue();

        if (!firstOther.IsSuccess)
            return firstOther.ErrorValue();

        if (!secondOther.IsSuccess)
            return secondOther.ErrorValue();

        if (!thirdOther.IsSuccess)
            return thirdOther.ErrorValue();

        return combine(
            SuccessValue(),
            firstOther.SuccessValue(),
            secondOther.SuccessValue(),
            thirdOther.SuccessValue()
        );
    }

    /// <summary>
    /// Combines this result with four others using the specified combining function.
    /// </summary>
    /// <param name="firstOther">The first result to combine with this one.</param>
    /// <param name="secondOther">The second result to combine with this one.</param>
    /// <param name="thirdOther">The third result to combine with this one.</param>
    /// <param name="fourthOther">The fourth result to combine with this one.</param>
    /// <param name="combine">A function to combine the success values.</param>
    public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther>(
        Result<TError, TSuccessFirstOther> firstOther,
        Result<TError, TSuccessSecondOther> secondOther,
        Result<TError, TSuccessThirdOther> thirdOther,
        Result<TError, TSuccessFourthOther> fourthOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther, TSuccessOut> combine
    ) {
        if (!IsSuccess)
            return ErrorValue();

        if (!firstOther.IsSuccess)
            return firstOther.ErrorValue();

        if (!secondOther.IsSuccess)
            return secondOther.ErrorValue();

        if (!thirdOther.IsSuccess)
            return thirdOther.ErrorValue();

        if (!fourthOther.IsSuccess)
            return fourthOther.ErrorValue();

        return combine(
            SuccessValue(),
            firstOther.SuccessValue(),
            secondOther.SuccessValue(),
            thirdOther.SuccessValue(),
            fourthOther.SuccessValue()
        );
    }
}
