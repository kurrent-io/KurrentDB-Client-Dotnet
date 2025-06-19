namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    #region . sync .

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain validation and processing operations
    /// Result&lt;string, ValidationError&gt; emailValidation = ValidateEmail(input);
    /// 
    /// Result&lt;User, ValidationError&gt; userCreation = emailValidation.Then(
    ///     validEmail => CreateUser(validEmail)  // Returns Result&lt;User, ValidationError&gt;
    /// );
    /// 
    /// // Alternative: Chain multiple operations in a pipeline
    /// Result&lt;OrderConfirmation, ValidationError&gt; orderResult = ValidateOrder(orderData)
    ///     .Then(order => ProcessPayment(order.PaymentInfo))
    ///     .Then(payment => CreateOrderConfirmation(payment.TransactionId));
    /// 
    /// // If any step fails, the error is propagated through the entire chain
    /// </code>
    /// </example>
    public Result<TOut, TError> Then<TOut>(Func<TValue, Result<TOut, TError>> binder) where TOut : notnull =>
        IsSuccess ? binder(Value) : Error;

    /// <summary>
    /// Chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">A function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>The result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain operations with shared configuration context
    /// Result&lt;FileData, ProcessingError&gt; fileValidation = ValidateFile(uploadedFile);
    /// var processingOptions = new ProcessingOptions { 
    ///     MaxSize = 5 * 1024 * 1024, // 5MB
    ///     AllowedFormats = new[] { "jpg", "png", "gif" }
    /// };
    /// 
    /// Result&lt;ProcessedFile, ProcessingError&gt; processedFile = fileValidation.Then(
    ///     binder: (fileData, options) => ProcessAndOptimizeFile(fileData, options),
    ///     state: processingOptions
    /// );
    /// 
    /// // The processing options are passed through the chain
    /// </code>
    /// </example>
    public Result<TOut, TError> Then<TOut, TState>(Func<TValue, TState, Result<TOut, TError>> binder, TState state) where TOut : notnull =>
        IsSuccess ? binder(Value, state) : Error;

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function.
    /// If this result is an error, the error is propagated.
    /// This is also known as 'flatMap' or 'SelectMany'.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain async operations in a user registration workflow
    /// Result&lt;UserData, ValidationError&gt; validationResult = ValidateUserInput(request);
    /// 
    /// Result&lt;User, ValidationError&gt; registrationResult = await validationResult
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
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut>(Func<TValue, ValueTask<Result<TOut, TError>>> binder) where TOut : notnull =>
        IsSuccess ? await binder(Value).ConfigureAwait(false) : Error;

    /// <summary>
    /// Asynchronously chains a new operation based on the success value of this result using the specified binding function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the success value of the result returned by the binder.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the binder.</typeparam>
    /// <param name="binder">An asynchronous function that takes the success value and state, and returns a new <see cref="Result{TOut, TError}"/>.</param>
    /// <param name="state">The state to pass to the binder.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the result of the <paramref name="binder"/> function if this result is a success; otherwise, a new result with the original error.</returns>
    /// <example>
    /// <code>
    /// // Chain async operations with database transaction context
    /// Result&lt;Order, BusinessError&gt; orderValidation = ValidateOrder(orderRequest);
    /// using var transaction = await dbContext.Database.BeginTransactionAsync();
    /// 
    /// Result&lt;OrderConfirmation, BusinessError&gt; finalResult = await orderValidation
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
    public async ValueTask<Result<TOut, TError>> ThenAsync<TOut, TState>(Func<TValue, TState, ValueTask<Result<TOut, TError>>> binder, TState state) where TOut : notnull =>
        IsSuccess ? await binder(Value, state).ConfigureAwait(false) : Error;

    #endregion
}
