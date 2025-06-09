// #nullable enable
//
// using System.Diagnostics.CodeAnalysis;
// using OneOf;
// using OneOf.Types;
//
// namespace KurrentDB.Client;
//
// /// <summary>
// /// Represents a discriminated union of an error or success value,
// /// providing a functional approach to error handling.
// /// </summary>
// /// <typeparam name="TError">The type of the error value.</typeparam>
// /// <typeparam name="TSuccess">The type of the success value.</typeparam>
// [PublicAPI]
// public class Result<TError, TSuccess> : OneOfBase<Error<TError>, Success<TSuccess>> {
//     /// <summary>
//     /// Creates a new Result from a OneOf discriminated union.
//     /// </summary>
//     /// <param name="_">The discriminated union value.</param>
//     public Result(OneOf<Error<TError>, Success<TSuccess>> _) : base(_) { }
//
//     /// <summary>
//     /// Protected constructor for derived classes that need to set up the internal state differently.
//     /// </summary>
//     /// <param name="value">The value (either success or error).</param>
//     /// <param name="isSuccess">Whether this is a success result.</param>
//     protected Result(object value, bool isSuccess) : base(
//         isSuccess
//             ? OneOf<Error<TError>, Success<TSuccess>>.FromT1(new Success<TSuccess>((TSuccess)value))
//             : OneOf<Error<TError>, Success<TSuccess>>.FromT0(new Error<TError>((TError)value))
//     ) { }
//
//     /// <summary>
//     /// Implicitly converts an Error to a Result.
//     /// </summary>
//     public static implicit operator Result<TError, TSuccess>(Error<TError> _) => new(_);
//
//     /// <summary>
//     /// Implicitly converts a Success to a Result.
//     /// </summary>
//     public static implicit operator Result<TError, TSuccess>(Success<TSuccess> _) => new(_);
//
//     /// <summary>
//     /// Implicitly converts a success value to a Result.
//     /// </summary>
//     public static implicit operator Result<TError, TSuccess>(TSuccess value) => new Success<TSuccess>(value);
//
//     /// <summary>
//     /// Implicitly converts an error value to a Result.
//     /// </summary>
//     public static implicit operator Result<TError, TSuccess>(TError value) => new Error<TError>(value);
//
//     /// <summary>
//     /// Creates an error result with the specified error value.
//     /// </summary>
//     public static Result<TError, TSuccess> Error(TError value) => new Error<TError>(value);
//
//     /// <summary>
//     /// Creates a success result with the specified success value.
//     /// </summary>
//     public static Result<TError, TSuccess> Success(TSuccess value) => new Success<TSuccess>(value);
//
//     /// <summary>
//     /// Creates a result from a function that might throw an exception.
//     /// </summary>
//     /// <param name="func">A function that returns a value or throws an exception.</param>
//     /// <param name="exceptionHandler">A function that transforms an exception into an error value.</param>
//     /// <returns>A result representing the function's outcome.</returns>
//     public static Result<TError, TSuccess> Try(Func<TSuccess> func, Func<Exception, TError> exceptionHandler) {
//         try {
//             return Success(func());
//         }
//         catch (Exception ex) {
//             return Error(exceptionHandler(ex));
//         }
//     }
//
//     /// <summary>
//     /// Creates a result from a nullable reference type.
//     /// </summary>
//     /// <param name="value">The value which might be null.</param>
//     /// <param name="errorIfNull">The error value to use if the value is null.</param>
//     /// <returns>A success result with the value if non-null, otherwise an error result.</returns>
//     public static Result<TError, T> FromNullable<T>(T? value, TError errorIfNull) =>
//         value is not null
//             ? Result<TError, T>.Success(value)
//             : Result<TError, T>.Error(errorIfNull);
//
//     /// <summary>
//     /// Creates a result from a nullable value type.
//     /// </summary>
//     /// <typeparam name="T">The type of the value.</typeparam>
//     /// <param name="value">The nullable value.</param>
//     /// <param name="errorIfNull">The error value to use if the value is null.</param>
//     /// <returns>A success result with the value if it has value, otherwise an error result.</returns>
//     public static Result<TError, T> FromNullableStruct<T>(T? value, TError errorIfNull) where T : struct =>
//         value.HasValue
//             ? Result<TError, T>.Success(value.Value)
//             : Result<TError, T>.Error(errorIfNull);
//
//     /// <summary>
//     /// Checks if the result represents an error.
//     /// </summary>
//     public bool IsError => IsT0;
//
//     /// <summary>
//     /// Checks if the result represents a success.
//     /// </summary>
//     public bool IsSuccess => IsT1;
//
//     /// <summary>
//     /// Gets the error value if this result represents an error.
//     /// </summary>
//     /// <exception cref="InvalidOperationException">Thrown when the result is a success.</exception>
//     public TError ErrorValue() =>
//         !IsError
//             ? throw new InvalidOperationException("Cannot access error value on a success result")
//             : AsT0.Value;
//
//     /// <summary>
//     /// Gets the success value if this result represents a success.
//     /// </summary>
//     /// <exception cref="InvalidOperationException">Thrown when the result is an error.</exception>
//     public TSuccess SuccessValue() =>
//         !IsSuccess
//             ? throw new InvalidOperationException("Cannot access success value on an error result")
//             : AsT1.Value;
//
//     /// <summary>
//     /// Attempts to get the success value from this result.
//     /// </summary>
//     /// <param name="value">The success value if present, default otherwise.</param>
//     /// <returns>True if the result is a success, false otherwise.</returns>
//     public bool TryGetSuccess([MaybeNullWhen(false)] out TSuccess value) {
//         value = IsSuccess ? AsT1.Value : default;
//         return IsSuccess;
//     }
//
//     /// <summary>
//     /// Attempts to get the error value from this result.
//     /// </summary>
//     /// <param name="value">The error value if present, default otherwise.</param>
//     /// <returns>True if the result is an error, false otherwise.</returns>
//     public bool TryGetError([MaybeNullWhen(false)] out TError value) {
//         value = IsError ? AsT0.Value : default;
//         return IsError;
//     }
//
//     /// <summary>
//     /// Transforms a success result using the specified binding function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="binder">A function that takes the success value and returns a new result.</param>
//     /// <returns>The result of the binding function, or an error if this result is an error.</returns>
//     public Result<TError, TOut> Bind<TOut>(Func<TSuccess, Result<TError, TOut>> binder) =>
//         Match<Result<TError, TOut>>(
//             error   => Result<TError, TOut>.Error(error.Value),
//             success => binder(success.Value)
//         );
//
//     /// <summary>
//     /// Transforms an error result using the specified binding function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output error value.</typeparam>
//     /// <param name="binder">A function that takes the error value and returns a new result.</param>
//     /// <returns>The result of the binding function, or a success if this result is a success.</returns>
//     public Result<TOut, TSuccess> BindError<TOut>(Func<TError, Result<TOut, TSuccess>> binder) =>
//         Match<Result<TOut, TSuccess>>(
//             error   => binder(error.Value),
//             success => Result<TOut, TSuccess>.Success(success.Value)
//         );
//
//     /// <summary>
//     /// Maps the success value using the specified mapping function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="mapSuccess">A function that transforms the success value.</param>
//     /// <returns>A new result with the mapped success value, or the original error.</returns>
//     public Result<TError, TOut> Map<TOut>(Func<TSuccess, TOut> mapSuccess) =>
//         Match<Result<TError, TOut>>(
//             error   => Result<TError, TOut>.Error(error.Value),
//             success => Result<TError, TOut>.Success(mapSuccess(success.Value))
//         );
//
//     /// <summary>
//     /// Maps the error value using the specified mapping function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output error value.</typeparam>
//     /// <param name="mapError">A function that transforms the error value.</param>
//     /// <returns>A new result with the mapped error value, or the original success.</returns>
//     public Result<TOut, TSuccess> MapError<TOut>(Func<TError, TOut> mapError) =>
//         Match<Result<TOut, TSuccess>>(
//             error   => Result<TOut, TSuccess>.Error(mapError(error.Value)),
//             success => Result<TOut, TSuccess>.Success(success.Value)
//         );
//
//     /// <summary>
//     /// Performs an action on the success value if this result is a success.
//     /// </summary>
//     /// <param name="action">The action to perform on the success value.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public Result<TError, TSuccess> Do(Action<TSuccess> action) {
//         if (IsSuccess)
//             action(SuccessValue());
//
//         return this;
//     }
//
//     /// <summary>
//     /// Performs an action on the error value if this result is an error.
//     /// </summary>
//     /// <param name="action">The action to perform on the error value.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public Result<TError, TSuccess> DoIfError(Action<TError> action) {
//         if (IsError)
//             action(ErrorValue());
//
//         return this;
//     }
//
//     /// <summary>
//     /// Returns the success value, or falls back to a value derived from the error.
//     /// </summary>
//     /// <param name="fallback">A function to transform the error into a success value.</param>
//     /// <returns>The success value or the fallback value.</returns>
//     public TSuccess DefaultWith(Func<TError, TSuccess> fallback) =>
//         Match<TSuccess>(
//             error   => fallback(error.Value),
//             success => success.Value
//         );
//
//     /// <summary>
//     /// Transforms this result into a value of a different type based on whether it's an error or success.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output value.</typeparam>
//     /// <param name="caseError">A function to transform the error value.</param>
//     /// <param name="caseSuccess">A function to transform the success value.</param>
//     /// <returns>The transformed value.</returns>
//     public TOut Fold<TOut>(Func<TError, TOut> caseError, Func<TSuccess, TOut> caseSuccess) =>
//         Match<TOut>(
//             error   => caseError(error.Value),
//             success => caseSuccess(success.Value)
//         );
//
//     /// <summary>
//     /// Combines this result with another using the specified combining function.
//     /// </summary>
//     /// <typeparam name="TSuccessOut">The output success type.</typeparam>
//     /// <typeparam name="TSuccessOther">The success type of the other result.</typeparam>
//     /// <param name="other">The result to combine with this one.</param>
//     /// <param name="combine">A function to combine the success values.</param>
//     /// <returns>A result containing either the combined success values or the first error encountered.</returns>
//     public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessOther>(
//         Result<TError, TSuccessOther> other,
//         Func<TSuccess, TSuccessOther, TSuccessOut> combine
//     ) {
//         if (!IsSuccess)
//             return ErrorValue();
//
//         if (!other.IsSuccess)
//             return other.ErrorValue();
//
//         return combine(SuccessValue(), other.SuccessValue());
//     }
//
//     /// <summary>
//     /// Combines this result with two others using the specified combining function.
//     /// </summary>
//     /// <param name="firstOther">The first result to combine with this one.</param>
//     /// <param name="secondOther">The second result to combine with this one.</param>
//     /// <param name="combine">A function to combine the success values.</param>
//     /// <returns>A result containing either the combined success values or the first error encountered.</returns>
//     public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther>(
//         Result<TError, TSuccessFirstOther> firstOther,
//         Result<TError, TSuccessSecondOther> secondOther,
//         Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessOut> combine
//     ) {
//         if (!IsSuccess)
//             return ErrorValue();
//
//         if (!firstOther.IsSuccess)
//             return firstOther.ErrorValue();
//
//         if (!secondOther.IsSuccess)
//             return secondOther.ErrorValue();
//
//         return combine(SuccessValue(), firstOther.SuccessValue(), secondOther.SuccessValue());
//     }
//
//     /// <summary>
//     /// Combines this result with three others using the specified combining function.
//     /// </summary>
//     /// <param name="firstOther">The first result to combine with this one.</param>
//     /// <param name="secondOther">The second result to combine with this one.</param>
//     /// <param name="thirdOther">The third result to combine with this one.</param>
//     /// <param name="combine">A function to combine the success values.</param>
//     /// <returns>A result containing either the combined success values or the first error encountered.</returns>
//     public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther>(
//         Result<TError, TSuccessFirstOther> firstOther,
//         Result<TError, TSuccessSecondOther> secondOther,
//         Result<TError, TSuccessThirdOther> thirdOther,
//         Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessOut> combine
//     ) {
//         if (!IsSuccess)
//             return ErrorValue();
//
//         if (!firstOther.IsSuccess)
//             return firstOther.ErrorValue();
//
//         if (!secondOther.IsSuccess)
//             return secondOther.ErrorValue();
//
//         if (!thirdOther.IsSuccess)
//             return thirdOther.ErrorValue();
//
//         return combine(
//             SuccessValue(),
//             firstOther.SuccessValue(),
//             secondOther.SuccessValue(),
//             thirdOther.SuccessValue()
//         );
//     }
//
//     /// <summary>
//     /// Combines this result with four others using the specified combining function.
//     /// </summary>
//     /// <param name="firstOther">The first result to combine with this one.</param>
//     /// <param name="secondOther">The second result to combine with this one.</param>
//     /// <param name="thirdOther">The third result to combine with this one.</param>
//     /// <param name="fourthOther">The fourth result to combine with this one.</param>
//     /// <param name="combine">A function to combine the success values.</param>
//     /// <returns>A result containing either the combined success values or the first error encountered.</returns>
//     public Result<TError, TSuccessOut> Zip<TSuccessOut, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther>(
//         Result<TError, TSuccessFirstOther> firstOther,
//         Result<TError, TSuccessSecondOther> secondOther,
//         Result<TError, TSuccessThirdOther> thirdOther,
//         Result<TError, TSuccessFourthOther> fourthOther,
//         Func<TSuccess, TSuccessFirstOther, TSuccessSecondOther, TSuccessThirdOther, TSuccessFourthOther, TSuccessOut> combine
//     ) {
//         if (!IsSuccess)
//             return ErrorValue();
//
//         if (!firstOther.IsSuccess)
//             return firstOther.ErrorValue();
//
//         if (!secondOther.IsSuccess)
//             return secondOther.ErrorValue();
//
//         if (!thirdOther.IsSuccess)
//             return thirdOther.ErrorValue();
//
//         if (!fourthOther.IsSuccess)
//             return fourthOther.ErrorValue();
//
//         return combine(
//             SuccessValue(),
//             firstOther.SuccessValue(),
//             secondOther.SuccessValue(),
//             thirdOther.SuccessValue(),
//             fourthOther.SuccessValue()
//         );
//     }
//
//     #region . Task .
//
//     /// <summary>
//     /// Asynchronously transforms a success result using the specified binding function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="binder">An asynchronous function that takes the success value and returns a new result.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The result of the binding function, or an error if this result is an error.</returns>
//     public async Task<Result<TError, TOut>> BindAsync<TOut>(Func<TSuccess, Task<Result<TError, TOut>>> binder, CancellationToken cancellationToken = default) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         return await Match<Task<Result<TError, TOut>>>(
//             error => Task.FromResult(Result<TError, TOut>.Error(error.Value)),
//             async success => await binder(success.Value).ConfigureAwait(false)
//         ).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously maps the success value using the specified mapping function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="mapper">An asynchronous function that transforms the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A new result with the mapped success value, or the original error.</returns>
//     public async Task<Result<TError, TOut>> MapAsync<TOut>(Func<TSuccess, Task<TOut>> mapper, CancellationToken cancellationToken = default) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         return await Match<Task<Result<TError, TOut>>>(
//             error => Task.FromResult(Result<TError, TOut>.Error(error.Value)),
//             async success => {
//                 var mappedValue = await mapper(success.Value).ConfigureAwait(false);
//                 return Result<TError, TOut>.Success(mappedValue);
//             }
//         ).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously performs an action on the success value if this result is a success.
//     /// </summary>
//     /// <param name="action">The asynchronous action to perform on the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public async Task<Result<TError, TSuccess>> DoAsync(Func<TSuccess, Task> action, CancellationToken cancellationToken = default) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (IsSuccess)
//             await action(SuccessValue()).ConfigureAwait(false);
//
//         return this;
//     }
//
//     /// <summary>
//     /// Asynchronously performs an action on the error value if this result is an error.
//     /// </summary>
//     /// <param name="action">The asynchronous action to perform on the error value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public async Task<Result<TError, TSuccess>> DoIfErrorAsync(Func<TError, Task> action, CancellationToken cancellationToken = default) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (IsError)
//             await action(ErrorValue()).ConfigureAwait(false);
//
//         return this;
//     }
//
//     /// <summary>
//     /// Asynchronously transforms this result into a value of a different type.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output value.</typeparam>
//     /// <param name="caseError">An asynchronous function to transform the error value.</param>
//     /// <param name="caseSuccess">An asynchronous function to transform the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The transformed value.</returns>
//     public async Task<TOut> FoldAsync<TOut>(
//         Func<TError, Task<TOut>> caseError,
//         Func<TSuccess, Task<TOut>> caseSuccess,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         return await Match<Task<TOut>>(
//             async error   => await caseError(error.Value).ConfigureAwait(false),
//             async success => await caseSuccess(success.Value).ConfigureAwait(false)
//         ).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Creates a result from a task, catching any exceptions as errors.
//     /// </summary>
//     /// <param name="task">The task that may produce a value or throw an exception.</param>
//     /// <param name="exceptionHandler">A function to transform an exception into an error value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A result representing the task's outcome.</returns>
//     public static async Task<Result<TError, TSuccess>> FromTaskAsync(
//         Task<TSuccess> task,
//         Func<Exception, TError> exceptionHandler,
//         CancellationToken cancellationToken = default
//     ) {
//         try {
//             cancellationToken.ThrowIfCancellationRequested();
//             var result = await task.ConfigureAwait(false);
//             return Success(result);
//         }
//         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
//             throw;
//         }
//         catch (Exception ex) {
//             return Error(exceptionHandler(ex));
//         }
//     }
//
//     #endregion
//
//     #region . ValueTask .
//
//     /// <summary>
//     /// Asynchronously transforms a success result using the specified ValueTask binding function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="binder">An asynchronous function that takes the success value and returns a new result.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The result of the binding function, or an error if this result is an error.</returns>
//     public ValueTask<Result<TError, TOut>> BindValueAsync<TOut>(
//         Func<TSuccess, ValueTask<Result<TError, TOut>>> binder,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (IsError)
//             return ValueTask.FromResult(Result<TError, TOut>.Error(ErrorValue())); // new ValueTask<Result<TError, TOut>>(Result<TError, TOut>.Error(ErrorValue()));
//
//         return BindValueSuccessAsync(binder, cancellationToken);
//     }
//
//     async ValueTask<Result<TError, TOut>> BindValueSuccessAsync<TOut>(Func<TSuccess, ValueTask<Result<TError, TOut>>> binder, CancellationToken cancellationToken) {
//         cancellationToken.ThrowIfCancellationRequested();
//         return await binder(SuccessValue()).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously maps the success value using the specified ValueTask mapping function.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output success value.</typeparam>
//     /// <param name="mapper">An asynchronous function that transforms the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A new result with the mapped success value, or the original error.</returns>
//     public ValueTask<Result<TError, TOut>> MapValueAsync<TOut>(
//         Func<TSuccess, ValueTask<TOut>> mapper,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (IsError)
//             return new ValueTask<Result<TError, TOut>>(Result<TError, TOut>.Error(ErrorValue()));
//
//         return MapValueSuccessAsync(mapper, cancellationToken);
//     }
//
//     async ValueTask<Result<TError, TOut>> MapValueSuccessAsync<TOut>(Func<TSuccess, ValueTask<TOut>> mapper, CancellationToken cancellationToken) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var mappedValue = await mapper(SuccessValue()).ConfigureAwait(false);
//         return Result<TError, TOut>.Success(mappedValue);
//     }
//
//     /// <summary>
//     /// Asynchronously performs an action on the success value if this result is a success.
//     /// </summary>
//     /// <param name="action">The asynchronous action to perform on the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public ValueTask<Result<TError, TSuccess>> DoValueAsync(Func<TSuccess, ValueTask> action, CancellationToken cancellationToken = default) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (!IsSuccess)
//             return new ValueTask<Result<TError, TSuccess>>(this);
//
//         return DoValueSuccessAsync(action, cancellationToken);
//     }
//
//     private async ValueTask<Result<TError, TSuccess>> DoValueSuccessAsync(
//         Func<TSuccess, ValueTask> action,
//         CancellationToken cancellationToken
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         await action(SuccessValue()).ConfigureAwait(false);
//         return this;
//     }
//
//     /// <summary>
//     /// Asynchronously performs an action on the error value if this result is an error.
//     /// </summary>
//     /// <param name="action">The asynchronous action to perform on the error value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>This result instance for chaining.</returns>
//     public ValueTask<Result<TError, TSuccess>> DoIfErrorValueAsync(
//         Func<TError, ValueTask> action,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         if (!IsError)
//             return new ValueTask<Result<TError, TSuccess>>(this);
//
//         return DoIfErrorValueErrorAsync(action, cancellationToken);
//     }
//
//     private async ValueTask<Result<TError, TSuccess>> DoIfErrorValueErrorAsync(
//         Func<TError, ValueTask> action,
//         CancellationToken cancellationToken
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         await action(ErrorValue()).ConfigureAwait(false);
//         return this;
//     }
//
//     /// <summary>
//     /// Asynchronously transforms this result into a value of a different type using ValueTasks.
//     /// </summary>
//     /// <typeparam name="TOut">The type of the output value.</typeparam>
//     /// <param name="caseError">An asynchronous function to transform the error value.</param>
//     /// <param name="caseSuccess">An asynchronous function to transform the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The transformed value.</returns>
//     public ValueTask<TOut> FoldValueAsync<TOut>(
//         Func<TError, ValueTask<TOut>> caseError,
//         Func<TSuccess, ValueTask<TOut>> caseSuccess,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//
//         return IsError
//             ? FoldValueErrorAsync(caseError, cancellationToken)
//             : FoldValueSuccessAsync(caseSuccess, cancellationToken);
//     }
//
//     private async ValueTask<TOut> FoldValueErrorAsync<TOut>(
//         Func<TError, ValueTask<TOut>> caseError,
//         CancellationToken cancellationToken
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         return await caseError(ErrorValue()).ConfigureAwait(false);
//     }
//
//     private async ValueTask<TOut> FoldValueSuccessAsync<TOut>(
//         Func<TSuccess, ValueTask<TOut>> caseSuccess,
//         CancellationToken cancellationToken
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         return await caseSuccess(SuccessValue()).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Creates a result from a ValueTask, catching any exceptions as errors.
//     /// </summary>
//     /// <param name="task">The ValueTask that may produce a value or throw an exception.</param>
//     /// <param name="exceptionHandler">A function to transform an exception into an error value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A result representing the task's outcome.</returns>
//     public static async ValueTask<Result<TError, TSuccess>> FromValueTaskAsync(
//         ValueTask<TSuccess> task,
//         Func<Exception, TError> exceptionHandler,
//         CancellationToken cancellationToken = default
//     ) {
//         try {
//             cancellationToken.ThrowIfCancellationRequested();
//             var result = await task.ConfigureAwait(false);
//             return Success(result);
//         }
//         catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
//             throw;
//         }
//         catch (Exception ex) {
//             return Error(exceptionHandler(ex));
//         }
//     }
//
//     #endregion
// }
//
// /// <summary>
// /// Extension methods for Result objects with Task and ValueTask integration.
// /// </summary>
// [PublicAPI]
// public static class ResultExtensions {
//     #region . Task .
//
//     /// <summary>
//     /// Asynchronously transforms a success result from a Task using the specified binding function.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output success type.</typeparam>
//     /// <param name="resultTask">The task producing a result.</param>
//     /// <param name="binder">An asynchronous function that takes a success value and returns a new result.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The result of the binding function, or an error if the input result is an error.</returns>
//     public static async Task<Result<TError, TOut>> BindAsync<TError, TSuccess, TOut>(
//         this Task<Result<TError, TSuccess>> resultTask,
//         Func<TSuccess, Task<Result<TError, TOut>>> binder,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.BindAsync(binder, cancellationToken).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously maps the success value of a result from a Task.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output success type.</typeparam>
//     /// <param name="resultTask">The task producing a result.</param>
//     /// <param name="mapper">An asynchronous function that transforms the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A new result with the mapped success value, or the original error.</returns>
//     public static async Task<Result<TError, TOut>> MapAsync<TError, TSuccess, TOut>(
//         this Task<Result<TError, TSuccess>> resultTask,
//         Func<TSuccess, Task<TOut>> mapper,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.MapAsync(mapper, cancellationToken).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously transforms a result from a Task into a value of a different type.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output type.</typeparam>
//     /// <param name="resultTask">The task producing a result.</param>
//     /// <param name="caseError">An asynchronous function to transform the error value.</param>
//     /// <param name="caseSuccess">An asynchronous function to transform the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The transformed value.</returns>
//     public static async Task<TOut> FoldAsync<TError, TSuccess, TOut>(
//         this Task<Result<TError, TSuccess>> resultTask,
//         Func<TError, Task<TOut>> caseError,
//         Func<TSuccess, Task<TOut>> caseSuccess,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.FoldAsync(caseError, caseSuccess, cancellationToken).ConfigureAwait(false);
//     }
//
//     #endregion
//
//     #region . ValueTask .
//
//     /// <summary>
//     /// Asynchronously transforms a success result from a ValueTask using the specified binding function.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output success type.</typeparam>
//     /// <param name="resultTask">The ValueTask producing a result.</param>
//     /// <param name="binder">An asynchronous function that takes a success value and returns a new result.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The result of the binding function, or an error if the input result is an error.</returns>
//     public static async ValueTask<Result<TError, TOut>> BindValueAsync<TError, TSuccess, TOut>(
//         this ValueTask<Result<TError, TSuccess>> resultTask,
//         Func<TSuccess, ValueTask<Result<TError, TOut>>> binder,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.BindValueAsync(binder, cancellationToken).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously maps the success value of a result from a ValueTask.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output success type.</typeparam>
//     /// <param name="resultTask">The ValueTask producing a result.</param>
//     /// <param name="mapper">An asynchronous function that transforms the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>A new result with the mapped success value, or the original error.</returns>
//     public static async ValueTask<Result<TError, TOut>> MapValueAsync<TError, TSuccess, TOut>(
//         this ValueTask<Result<TError, TSuccess>> resultTask,
//         Func<TSuccess, ValueTask<TOut>> mapper,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.MapValueAsync(mapper, cancellationToken).ConfigureAwait(false);
//     }
//
//     /// <summary>
//     /// Asynchronously transforms a result from a ValueTask into a value of a different type.
//     /// </summary>
//     /// <typeparam name="TError">The error type.</typeparam>
//     /// <typeparam name="TSuccess">The success type.</typeparam>
//     /// <typeparam name="TOut">The output type.</typeparam>
//     /// <param name="resultTask">The ValueTask producing a result.</param>
//     /// <param name="caseError">An asynchronous function to transform the error value.</param>
//     /// <param name="caseSuccess">An asynchronous function to transform the success value.</param>
//     /// <param name="cancellationToken">A token to cancel the operation.</param>
//     /// <returns>The transformed value.</returns>
//     public static async ValueTask<TOut> FoldValueAsync<TError, TSuccess, TOut>(
//         this ValueTask<Result<TError, TSuccess>> resultTask,
//         Func<TError, ValueTask<TOut>> caseError,
//         Func<TSuccess, ValueTask<TOut>> caseSuccess,
//         CancellationToken cancellationToken = default
//     ) {
//         cancellationToken.ThrowIfCancellationRequested();
//         var result = await resultTask.ConfigureAwait(false);
//
//         return await result.FoldValueAsync(caseError, caseSuccess, cancellationToken).ConfigureAwait(false);
//     }
//
//     #endregion
// }
