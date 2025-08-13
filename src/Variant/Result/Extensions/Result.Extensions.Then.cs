namespace Kurrent;

/// <summary>
/// Extension methods for monadic binding (then/flatMap) operations on IResult types.
/// These methods provide functional composition capabilities for chaining Result-returning operations.
/// </summary>
[PublicAPI]
public static class ResultThenExtensions {
    #region . sync .

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="result">The result to chain from.</param>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain validation and processing operations
    /// IResult&lt;string, ValidationError&gt; emailValidation = ValidateEmail(input);
    /// 
    /// IResult&lt;User, ValidationError&gt; userCreation = emailValidation.Then(
    ///     validEmail => CreateUser(validEmail)  // Returns Result&lt;User, ValidationError&gt;
    /// );
    /// 
    /// // Alternative: Chain multiple operations in a pipeline
    /// IResult&lt;OrderConfirmation, ValidationError&gt; orderResult = ValidateOrder(orderData)
    ///     .Then(order => ProcessPayment(order.PaymentInfo))
    ///     .Then(payment => CreateOrderConfirmation(payment.TransactionId));
    /// 
    /// // If any step fails, the error is propagated through the entire chain
    /// </code>
    /// </example>
    public static Result<TOut, TError> Then<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TValue, Result<TOut, TError>> binder) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? binder(result.Value) : result.Error;

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="result">The result to chain from.</param>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain operations with shared configuration context
    /// IResult&lt;FileData, ProcessingError&gt; fileValidation = ValidateFile(uploadedFile);
    /// var processingOptions = new ProcessingOptions { 
    ///     MaxSize = 5 * 1024 * 1024, // 5MB
    ///     AllowedFormats = new[] { "jpg", "png", "gif" }
    /// };
    /// 
    /// IResult&lt;ProcessedFile, ProcessingError&gt; processedFile = fileValidation.Then(
    ///     binder: (fileData, options) => ProcessAndOptimizeFile(fileData, options),
    ///     state: processingOptions
    /// );
    /// 
    /// // The processing options are passed through the chain
    /// </code>
    /// </example>
    public static Result<TOut, TError> Then<TValue, TError, TOut, TState>(this IResult<TValue, TError> result, Func<TValue, TState, Result<TOut, TError>> binder, TState state) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? binder(result.Value, state) : result.Error;

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="result">The result to chain from.</param>
    /// <param name="binder">An asynchronous function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain async operations in a user registration workflow
    /// IResult&lt;UserData, ValidationError&gt; validationResult = ValidateUserInput(request);
    /// 
    /// IResult&lt;User, ValidationError&gt; registrationResult = await validationResult
    ///     .ThenAsync(async userData => {
    ///         // Each step is async and returns a Result
    ///         var existingUser = await CheckUserExistsAsync(userData.Email).ConfigureAwait(false);
    ///         if (existingUser.IsSuccess)
    ///             return Result.Failure&lt;User, ValidationError&gt;(new ValidationError("Email already registered"));
    /// 
    ///         return await CreateUserAccountAsync(userData).ConfigureAwait(false);
    ///     })
    ///     .ConfigureAwait(false);
    /// 
    /// // Can also be chained with other async operations
    /// var finalResult = await registrationResult
    ///     .ThenAsync(user => SendWelcomeEmailAsync(user))
    ///     .ConfigureAwait(false);
    /// </code>
    /// </example>
    public static async ValueTask<Result<TOut, TError>> ThenAsync<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TValue, ValueTask<Result<TOut, TError>>> binder) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? await binder(result.Value).ConfigureAwait(false) : result.Error;

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="result">The result to chain from.</param>
    /// <param name="binder">An asynchronous function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain async operations with database transaction context
    /// IResult&lt;Order, BusinessError&gt; orderValidation = ValidateOrder(orderRequest);
    /// using var transaction = await dbContext.Database.BeginTransactionAsync();
    /// 
    /// IResult&lt;OrderConfirmation, BusinessError&gt; finalResult = await orderValidation
    ///     .ThenAsync(async (order, tx) => {
    ///         // Reserve inventory within transaction
    ///         var inventoryResult = await ReserveInventoryAsync(order.Items, tx).ConfigureAwait(false);
    ///         if (inventoryResult.IsFailure)
    ///             return inventoryResult.Error;
    /// 
    ///         // Process payment within same transaction context
    ///         return await ProcessPaymentAsync(order.Payment, tx).ConfigureAwait(false);
    ///     }, state: transaction)
    ///     .ConfigureAwait(false);
    /// 
    /// if (finalResult.IsSuccess)
    ///     await transaction.CommitAsync();
    /// else
    ///     await transaction.RollbackAsync();
    /// </code>
    /// </example>
    public static async ValueTask<Result<TOut, TError>> ThenAsync<TValue, TError, TOut, TState>(this IResult<TValue, TError> result, Func<TValue, TState, ValueTask<Result<TOut, TError>>> binder, TState state) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? await binder(result.Value, state).ConfigureAwait(false) : result.Error;

    #endregion
}