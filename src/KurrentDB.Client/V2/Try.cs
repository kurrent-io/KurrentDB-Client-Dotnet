using OneOf;
using OneOf.Types;

namespace KurrentDB.Client;

/// <summary>
/// Provides utility methods for executing code blocks and catching exceptions,
/// returning a <see cref="Try{TSuccess}"/> monad.
/// </summary>
[PublicAPI]
public static class JustTry {
    /// <summary>
    /// Executes the provided synchronous function and catches any exceptions.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="action">The synchronous function to execute.</param>
    /// <returns>A <see cref="Try{TSuccess}"/> containing either the result or the caught exception.</returns>
    public static Try<TSuccess> Catching<TSuccess>(Func<TSuccess> action) {
        try {
            return action();
        }
        catch (Exception ex) {
            return ex;
        }
    }

    /// <summary>
    /// Asynchronously executes the provided async function and catches any exceptions.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="action">The async function that may throw exceptions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a <see cref="Try{TSuccess}"/> with either the result or caught exception.</returns>
    public static async Task<Try<TSuccess>> CatchingAsync<TSuccess>(
        Func<Task<TSuccess>> action,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await action().ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Re-throw if cancellation was requested, allowing it to propagate as a cancellation.
            throw;
        }
        catch (Exception ex) {
            return ex;
        }
    }

    /// <summary>
    /// Asynchronously executes the provided <see cref="ValueTask{TResult}"/> function and catches any exceptions.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="action">The <see cref="ValueTask{TResult}"/> function that may throw exceptions.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing a <see cref="Try{TSuccess}"/> with either the result or caught exception.</returns>
    public static async ValueTask<Try<TSuccess>> CatchAsync<TSuccess>(
        Func<ValueTask<TSuccess>> action,
        CancellationToken cancellationToken = default
    ) {
        try {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await action().ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Re-throw if cancellation was requested, allowing it to propagate as a cancellation.
            throw;
        }
        catch (Exception ex) {
            return ex;
        }
    }
}

/// <summary>
/// Represents a computation that may either result in a value of type <typeparamref name="TSuccess"/>
/// or an <see cref="Exception"/>. It is a specialization of <see cref="Result{TError, TSuccess}"/>.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
[PublicAPI]
public class Try<TSuccess> : Result<Exception, TSuccess> {
    /// <summary>
    /// Initializes a new instance of the <see cref="Try{TSuccess}"/> class from a OneOf discriminated union.
    /// </summary>
    /// <param name="value">The discriminated union value, either an <see cref="Error{TError}"/> of <see cref="Exception"/> or a <see cref="Success{TSuccess}"/>.</param>
    public Try(OneOf<Error<Exception>, Success<TSuccess>> value) : base(value) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Try{TSuccess}"/> class from a success value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public Try(TSuccess value) : base(value, true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Try{TSuccess}"/> class from an exception.
    /// </summary>
    /// <param name="error">The exception that occurred.</param>
    public Try(Exception error) : base(error, false) { }

    /// <summary>
    /// Implicitly converts an <see cref="Error{TError}"/> of <see cref="Exception"/> to a <see cref="Try{TSuccess}"/>.
    /// </summary>
    /// <param name="error">The error value.</param>
    /// <returns>A new <see cref="Try{TSuccess}"/> instance representing an error.</returns>
    public static implicit operator Try<TSuccess>(Error<Exception> error) => new(error);

    /// <summary>
    /// Implicitly converts a <see cref="Success{TSuccess}"/> to a <see cref="Try{TSuccess}"/>.
    /// </summary>
    /// <param name="success">The success value.</param>
    /// <returns>A new <see cref="Try{TSuccess}"/> instance representing a success.</returns>
    public static implicit operator Try<TSuccess>(Success<TSuccess> success) => new(success);

    /// <summary>
    /// Implicitly converts a <typeparamref name="TSuccess"/> value to a <see cref="Try{TSuccess}"/>.
    /// </summary>
    /// <param name="successValue">The success value.</param>
    /// <returns>A new <see cref="Try{TSuccess}"/> instance representing a success.</returns>
    public static implicit operator Try<TSuccess>(TSuccess successValue) => new(successValue);

    /// <summary>
    /// Implicitly converts an <see cref="Exception"/> to a <see cref="Try{TSuccess}"/>.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A new <see cref="Try{TSuccess}"/> instance representing an error.</returns>
    public static implicit operator Try<TSuccess>(Exception exception) => new(exception);

    /// <summary>
    /// Converts this <see cref="Try{TSuccess}"/> instance to its base <see cref="Result{TError, TSuccess}"/> type.
    /// </summary>
    /// <returns>A new <see cref="Result{TError, TSuccess}"/> instance with the same state.</returns>
    public Result<Exception, TSuccess> ToResult() {
        if (IsSuccess) {
            // This relies on Result<Exception, TSuccess> having an implicit operator from TSuccess
            return SuccessValue();
        }
        else {
            // This relies on Result<Exception, TSuccess> having an implicit operator from Exception
            return ErrorValue();
        }
    }

    /// <summary>
    /// Folds this <see cref="Try{TSuccess}"/> into a new <see cref="Try{TOut}"/> by applying the <paramref name="mapper"/> function
    /// to the success value and catching any exceptions thrown by the mapper.
    /// If this instance is an error, the original exception is propagated to the new <see cref="Try{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the resulting <see cref="Try{TOut}"/>.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Try{TOut}"/> representing the result of the mapping or any caught exception.</returns>
    public Try<TOut> FoldCatching<TOut>(Func<TSuccess, TOut> mapper) =>
        Fold(
            error => new Try<TOut>(error),                     // Propagate existing error
            success => JustTry.Catching(() => mapper(success)) // Catch new errors from mapper
        );

    /// <summary>
    /// Chains a computation that returns a <see cref="Try{TOut}"/> if this instance is a success.
    /// If this instance is an error, the error is propagated.
    /// Exceptions thrown by the <paramref name="binder"/> itself are caught and returned as an error <see cref="Try{TOut}"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the <see cref="Try{TOut}"/> returned by the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a <see cref="Try{TOut}"/>.</param>
    /// <returns>The <see cref="Try{TOut}"/> returned by the binder, or a new error <see cref="Try{TOut}"/> if an exception occurred.</returns>
    public new Try<TOut> Then<TOut>(Func<TSuccess, Try<TOut>> binder) {
        return Match<Try<TOut>>(
            error => new Try<TOut>(error.Value),
            success => {
                try {
                    return binder(success.Value);
                }
                catch (Exception ex) {
                    return new Try<TOut>(ex);
                }
            }
        );
    }

    /// <summary>
    /// Maps the success value of this <see cref="Try{TSuccess}"/> to a new <see cref="Try{TOut}"/> using the specified mapper function.
    /// Exceptions thrown by the <paramref name="mapper"/> are caught and returned as an error <see cref="Try{TOut}"/>.
    /// If this instance is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the resulting <see cref="Try{TOut}"/>.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Try{TOut}"/> representing the mapped value or any caught exception.</returns>
    public new Try<TOut> Map<TOut>(Func<TSuccess, TOut> mapper) {
        return Match<Try<TOut>>(
            error => new Try<TOut>(error.Value),
            success => JustTry.Catching(() => mapper(success.Value))
        );
    }

    /// <summary>
    /// Performs an action on the success value if this instance represents a success.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <returns>This <see cref="Try{TSuccess}"/> instance.</returns>
    public new Try<TSuccess> OnSuccess(Action<TSuccess> action) {
        // Base implementation returns 'this', which is already Try<TSuccess>
        base.OnSuccess(action);
        return this;
    }

    /// <summary>
    /// Performs an action on the error value (exception) if this instance represents an error.
    /// </summary>
    /// <param name="action">The action to perform.</param>
    /// <returns>This <see cref="Try{TSuccess}"/> instance.</returns>
    public new Try<TSuccess> OnError(Action<Exception> action) {
        // Base implementation returns 'this', which is already Try<TSuccess>
        base.OnError(action);
        return this;
    }

    /// <summary>
    /// Combines this <see cref="Try{TSuccess}"/> with another <see cref="Try{TSuccessOther}"/> using a combining function
    /// if both are successful. If either is an error, the first encountered error is propagated.
    /// Exceptions thrown by the <paramref name="combine"/> function are caught.
    /// </summary>
    /// <typeparam name="TSuccessOut">The type of the success value of the resulting <see cref="Try{TSuccessOut}"/>.</typeparam>
    /// <typeparam name="TSuccessOther">The type of the success value of the other <see cref="Try{TSuccessOther}"/>.</typeparam>
    /// <param name="other">The other <see cref="Try{TSuccessOther}"/> to combine with.</param>
    /// <param name="combine">A function to combine the success values.</param>
    /// <returns>A new <see cref="Try{TSuccessOut}"/> representing the combined value or any caught exception/propagated error.</returns>
    public Try<TSuccessOut> Zip<TSuccessOut, TSuccessOther>(
        Try<TSuccessOther> other,
        Func<TSuccess, TSuccessOther, TSuccessOut> combine
    ) {
        if (IsError) return new Try<TSuccessOut>(ErrorValue());
        if (other.IsError) return new Try<TSuccessOut>(other.ErrorValue());

        return JustTry.Catching(() => combine(SuccessValue(), other.SuccessValue()));
    }

    /// <summary>
    /// Combines this <see cref="Try{TSuccess}"/> with two other <see cref="Try{T}"/> instances.
    /// (Further documentation similar to the above Zip method)
    /// </summary>
    public Try<TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther>(
        Try<TSuccessFirstOther> firstOther,
        Try<TSuccessSecondOther> secondOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessOut> combine
    ) {
        if (IsError) return new Try<TSuccessOut>(ErrorValue());
        if (firstOther.IsError) return new Try<TSuccessOut>(firstOther.ErrorValue());
        if (secondOther.IsError) return new Try<TSuccessOut>(secondOther.ErrorValue());

        return JustTry.Catching(() => combine(SuccessValue(), firstOther.SuccessValue(), secondOther.SuccessValue()));
    }

    /// <summary>
    /// Combines this <see cref="Try{TSuccess}"/> with three other <see cref="Try{T}"/> instances.
    /// (Further documentation similar to the above Zip method)
    /// </summary>
    public Try<TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther>(
        Try<TSuccessFirstOther> firstOther,
        Try<TSuccessSecondOther> secondOther,
        Try<TSuccessThirdOther> thirdOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessOut> combine
    ) {
        if (IsError) return new Try<TSuccessOut>(ErrorValue());
        if (firstOther.IsError) return new Try<TSuccessOut>(firstOther.ErrorValue());
        if (secondOther.IsError) return new Try<TSuccessOut>(secondOther.ErrorValue());
        if (thirdOther.IsError) return new Try<TSuccessOut>(thirdOther.ErrorValue());

        return JustTry.Catching(() => combine(
                SuccessValue(), firstOther.SuccessValue(), secondOther.SuccessValue(),
                thirdOther.SuccessValue()
            )
        );
    }

    /// <summary>
    /// Combines this <see cref="Try{TSuccess}"/> with four other <see cref="Try{T}"/> instances.
    /// (Further documentation similar to the above Zip method)
    /// </summary>
    public Try<TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther>(
        Try<TSuccessFirstOther> firstOther,
        Try<TSuccessSecondOther> secondOther,
        Try<TSuccessThirdOther> thirdOther,
        Try<TSuccessFourthOther> fourthOther,
        Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther, TSuccessOut> combine
    ) {
        if (IsError) return new Try<TSuccessOut>(ErrorValue());
        if (firstOther.IsError) return new Try<TSuccessOut>(firstOther.ErrorValue());
        if (secondOther.IsError) return new Try<TSuccessOut>(secondOther.ErrorValue());
        if (thirdOther.IsError) return new Try<TSuccessOut>(thirdOther.ErrorValue());
        if (fourthOther.IsError) return new Try<TSuccessOut>(fourthOther.ErrorValue());

        return JustTry.Catching(() => combine(
                SuccessValue(), firstOther.SuccessValue(), secondOther.SuccessValue(),
                thirdOther.SuccessValue(), fourthOther.SuccessValue()
            )
        );
    }
}
