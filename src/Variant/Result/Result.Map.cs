namespace Kurrent;

public readonly partial record struct Result<TValue, TError> {
    // NOTE: All Map methods have been moved to IResultMapExtensions.cs
    // The extension methods now work on both Result<TValue, TError> and ResultBase<TValue, TError>
    // via the IResult<TValue, TError> interface.
    
    // Commented out original partial methods - extension methods provide the same functionality:
    
    #region . sync .

    /*
    /// <summary>
    /// Transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform user data to display format
    /// Result&lt;User, ValidationError&gt; userResult = ValidateUser(userData);
    /// 
    /// Result&lt;string, ValidationError&gt; displayNameResult = userResult.Map(
    ///     user => $"{user.FirstName} {user.LastName} ({user.Email})"
    /// );
    /// 
    /// // If userResult was successful: Result contains "John Doe (john@example.com)"
    /// // If userResult was an error: Error is propagated unchanged
    /// </code>
    /// </example>
    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> mapper) where TOut : notnull =>
        IsSuccess ? mapper(Value) : Error;

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">A function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform price with currency formatting
    /// Result&lt;decimal, ValidationError&gt; priceResult = ValidatePrice(input);
    /// var cultureInfo = new CultureInfo("en-US");
    /// 
    /// Result&lt;string, ValidationError&gt; formattedPriceResult = priceResult.Map(
    ///     mapper: (price, culture) => price.ToString("C", culture),
    ///     state: cultureInfo
    /// );
    /// 
    /// // If priceResult was successful: Result contains "$19.99"
    /// // If priceResult was an error: Error is propagated unchanged
    /// </code>
    /// </example>
    public Result<TOut, TError> Map<TOut, TState>(Func<TValue, TState, TOut> mapper, TState state) where TOut : notnull =>
        IsSuccess ? mapper(Value, state) : Error;

    /// <summary>
    /// Transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="mapper">A function to transform the error value.</param>
    /// <returns>A new <see cref="Result{TValue, TOut}"/> containing the original success value or the transformed error.</returns>
    /// <example>
    /// <code>
    /// // Convert validation errors to user-friendly messages
    /// Result&lt;User, ValidationError&gt; validationResult = ValidateUser(userData);
    /// 
    /// Result&lt;User, string&gt; userFriendlyResult = validationResult.MapError(
    ///     error => error.FieldName switch {
    ///         "Email" => "Please enter a valid email address",
    ///         "Age" => "Age must be between 13 and 120",
    ///         _ => "Please check your input and try again"
    ///     }
    /// );
    /// 
    /// // If validationResult was successful: User value is preserved
    /// // If validationResult was an error: Error becomes user-friendly string
    /// </code>
    /// </example>
    public Result<TValue, TOut> MapError<TOut>(Func<TError, TOut> mapper) where TOut : notnull =>
        IsSuccess ? Result.Success<TValue, TOut>(Value) : Result.Failure<TValue, TOut>(mapper(Error));
    */

    #endregion

    #region . async .

    /*
    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform user ID to full user profile via database lookup
    /// Result&lt;int, ValidationError&gt; userIdResult = ValidateUserId(input);
    /// 
    /// Result&lt;UserProfile, ValidationError&gt; userProfileResult = await userIdResult.MapAsync(
    ///     async userId => {
    ///         var profile = await userRepository.GetUserProfileAsync(userId).ConfigureAwait(false);
    ///         return new UserProfile {
    ///             Id = profile.Id,
    ///             DisplayName = profile.Name,
    ///             Avatar = profile.AvatarUrl
    ///         };
    ///     }
    /// ).ConfigureAwait(false);
    /// 
    /// // If userIdResult was successful: Result contains UserProfile from database
    /// // If userIdResult was an error: Error is propagated unchanged
    /// </code>
    /// </example>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut>(Func<TValue, ValueTask<TOut>> mapper) where TOut : notnull =>
        IsSuccess ? await mapper(Value).ConfigureAwait(false) : Error;

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform file path to processed file with configuration context
    /// Result&lt;string, ValidationError&gt; filePathResult = ValidateFilePath(input);
    /// var processingConfig = new ImageProcessingConfig { Quality = 85, Format = "webp" };
    /// 
    /// Result&lt;ProcessedImage, ValidationError&gt; processedImageResult = await filePathResult.MapAsync(
    ///     async (filePath, config) => {
    ///         var imageData = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
    ///         var processed = await imageProcessor.ProcessAsync(imageData, config).ConfigureAwait(false);
    ///         return new ProcessedImage { Data = processed, Format = config.Format };
    ///     },
    ///     state: processingConfig
    /// ).ConfigureAwait(false);
    /// </code>
    /// </example>
    public async ValueTask<Result<TOut, TError>> MapAsync<TOut, TState>(Func<TValue, TState, ValueTask<TOut>> mapper, TState state) where TOut : notnull =>
        IsSuccess ? await mapper(Value, state).ConfigureAwait(false) : Error;

    /// <summary>
    /// Asynchronously transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="mapper">An asynchronous function to transform the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    /// <example>
    /// <code>
    /// // Transform validation errors to localized messages via async lookup
    /// Result&lt;User, ValidationError&gt; validationResult = ValidateUser(userData);
    /// 
    /// Result&lt;User, string&gt; localizedResult = await validationResult.MapErrorAsync(
    ///     async error => {
    ///         var localizationKey = $"validation.{error.FieldName}.{error.Code}";
    ///         var localizedMessage = await localizationService
    ///             .GetLocalizedStringAsync(localizationKey, userLocale)
    ///             .ConfigureAwait(false);
    ///         return localizedMessage ?? error.DefaultMessage;
    ///     }
    /// ).ConfigureAwait(false);
    /// 
    /// // If validationResult was successful: User value is preserved
    /// // If validationResult was an error: Error becomes localized string
    /// </code>
    /// </example>
    public async ValueTask<Result<TValue, TOut>> MapErrorAsync<TOut>(Func<TError, ValueTask<TOut>> mapper) where TOut : notnull =>
        IsSuccess ? Value : await mapper(Error).ConfigureAwait(false);
    */

    #endregion
}
