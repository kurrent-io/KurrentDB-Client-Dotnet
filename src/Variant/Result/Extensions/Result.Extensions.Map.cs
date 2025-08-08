namespace Kurrent;

/// <summary>
/// Extension methods for mapping/transforming IResult types.
/// These methods provide functional transformation capabilities for both success values and error values.
/// </summary>
[PublicAPI]
public static class ResultMapExtensions {
    #region . sync .

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">A function to transform the success value.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform user data to display format
    /// IResult&lt;User, ValidationError&gt; userResult = ValidateUser(userData);
    /// 
    /// IResult&lt;string, ValidationError&gt; displayNameResult = userResult.Map(
    ///     user => $"{user.FirstName} {user.LastName} ({user.Email})"
    /// );
    /// 
    /// // If userResult was successful: Result contains "John Doe (john@example.com)"
    /// // If userResult was an error: Error is propagated unchanged
    /// </code>
    /// </example>
    public static Result<TOut, TError> Map<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TValue, TOut> mapper) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? mapper(result.Value) : result.Error;

    /// <summary>
    /// Transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">A function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A new <see cref="Result{TOut, TError}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform price with currency formatting
    /// IResult&lt;decimal, ValidationError&gt; priceResult = ValidatePrice(input);
    /// var cultureInfo = new CultureInfo("en-US");
    /// 
    /// IResult&lt;string, ValidationError&gt; formattedPriceResult = priceResult.Map(
    ///     mapper: (price, culture) => price.ToString("C", culture),
    ///     state: cultureInfo
    /// );
    /// 
    /// // If priceResult was successful: Result contains "$19.99"
    /// // If priceResult was an error: Error is propagated unchanged
    /// </code>
    /// </example>
    public static Result<TOut, TError> Map<TValue, TError, TOut, TState>(this IResult<TValue, TError> result, Func<TValue, TState, TOut> mapper, TState state) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? mapper(result.Value, state) : result.Error;

    /// <summary>
    /// Transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the current error value.</typeparam>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">A function to transform the error value.</param>
    /// <returns>A new <see cref="Result{TValue, TOut}"/> containing the original success value or the transformed error.</returns>
    /// <example>
    /// <code>
    /// // Convert validation errors to user-friendly messages
    /// IResult&lt;User, ValidationError&gt; validationResult = ValidateUser(userData);
    /// 
    /// IResult&lt;User, string&gt; userFriendlyResult = validationResult.MapError(
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
    public static Result<TValue, TOut> MapError<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TError, TOut> mapper) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? Result.Success<TValue, TOut>(result.Value) : Result.Failure<TValue, TOut>(mapper(result.Error));

    #endregion

    #region . async .

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">An asynchronous function to transform the success value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform user ID to full user profile via database lookup
    /// IResult&lt;int, ValidationError&gt; userIdResult = ValidateUserId(input);
    /// 
    /// IResult&lt;UserProfile, ValidationError&gt; userProfileResult = await userIdResult.MapAsync(
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
    public static async ValueTask<Result<TOut, TError>> MapAsync<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TValue, ValueTask<TOut>> mapper) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? await mapper(result.Value).ConfigureAwait(false) : result.Error;

    /// <summary>
    /// Asynchronously transforms the success value of this result using the specified mapping function, passing additional state.
    /// If this result is an error, the error is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the current success value.</typeparam>
    /// <typeparam name="TError">The type of the error value.</typeparam>
    /// <typeparam name="TOut">The type of the new success value.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the mapper.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">An asynchronous function to transform the success value, taking additional state.</param>
    /// <param name="state">The state to pass to the mapper.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the transformed success value or the original error.</returns>
    /// <example>
    /// <code>
    /// // Transform file path to processed file with configuration context
    /// IResult&lt;string, ValidationError&gt; filePathResult = ValidateFilePath(input);
    /// var processingConfig = new ImageProcessingConfig { Quality = 85, Format = "webp" };
    /// 
    /// IResult&lt;ProcessedImage, ValidationError&gt; processedImageResult = await filePathResult.MapAsync(
    ///     async (filePath, config) => {
    ///         var imageData = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
    ///         var processed = await imageProcessor.ProcessAsync(imageData, config).ConfigureAwait(false);
    ///         return new ProcessedImage { Data = processed, Format = config.Format };
    ///     },
    ///     state: processingConfig
    /// ).ConfigureAwait(false);
    /// </code>
    /// </example>
    public static async ValueTask<Result<TOut, TError>> MapAsync<TValue, TError, TOut, TState>(this IResult<TValue, TError> result, Func<TValue, TState, ValueTask<TOut>> mapper, TState state) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? await mapper(result.Value, state).ConfigureAwait(false) : result.Error;

    /// <summary>
    /// Asynchronously transforms the error value of this result using the specified mapping function.
    /// If this result is a success, the success value is propagated.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <typeparam name="TError">The type of the current error value.</typeparam>
    /// <typeparam name="TOut">The type of the new error value.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">An asynchronous function to transform the error value.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> containing the original success value or the transformed error.</returns>
    /// <example>
    /// <code>
    /// // Transform validation errors to localized messages via async lookup
    /// IResult&lt;User, ValidationError&gt; validationResult = ValidateUser(userData);
    /// 
    /// IResult&lt;User, string&gt; localizedResult = await validationResult.MapErrorAsync(
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
    public static async ValueTask<Result<TValue, TOut>> MapErrorAsync<TValue, TError, TOut>(this IResult<TValue, TError> result, Func<TError, ValueTask<TOut>> mapper) 
        where TValue : notnull where TError : notnull where TOut : notnull =>
        result.IsSuccess ? result.Value : await mapper(result.Error).ConfigureAwait(false);

    #endregion
}