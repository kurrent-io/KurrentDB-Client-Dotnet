// namespace Kurrent;
//
// /// <summary>
// /// Extension methods for pattern matching on IResult types.
// /// These methods provide exhaustive pattern matching capabilities for handling both success and error cases.
// /// </summary>
// [PublicAPI]
// public static class ResultMatchExtensions {
//     #region . sync .
//
//     /// <summary>
//     /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
//     /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
//     /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
//     /// <example>
//     /// <code>
//     /// // Parse user input and convert to display message
//     /// IResult&lt;int, string&gt; parseResult = TryParseAge(userInput);
//     ///
//     /// string message = parseResult.Match(
//     ///     onSuccess: age => $"You are {age} years old",
//     ///     onError: error => $"Invalid input: {error}"
//     /// );
//     ///
//     /// Console.WriteLine(message);
//     /// // Output: "You are 25 years old" or "Invalid input: Not a valid number"
//     /// </code>
//     /// </example>
//     public static TResult Match<TValue, TError, TResult>(this IResult<TValue, TError> result, Func<TValue, TResult> onSuccess, Func<TError, TResult> onError)
//         where TValue : notnull where TError : notnull =>
//         result.IsSuccess ? onSuccess(result.Value) : onError(result.Error);
//
//     /// <summary>
//     /// Executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value and state as input.</param>
//     /// <param name="onError">The function to execute if the result is an error. It takes the error value and state as input.</param>
//     /// <param name="state">The state to pass to the functions.</param>
//     /// <returns>The value returned by the executed function (<paramref name="onSuccess"/> or <paramref name="onError"/>).</returns>
//     /// <example>
//     /// <code>
//     /// // Process user registration with localization context
//     /// IResult&lt;User, ValidationError&gt; registrationResult = ValidateUser(userData);
//     /// var locale = "en-US";
//     ///
//     /// string localizedMessage = registrationResult.Match(
//     ///     onSuccess: (user, lang) => GetLocalizedWelcome(user.Name, lang),
//     ///     onError: (error, lang) => GetLocalizedError(error.Code, lang),
//     ///     state: locale
//     /// );
//     ///
//     /// return localizedMessage;
//     /// // Output: "Welcome John!" or "El nombre es requerido" (depending on locale)
//     /// </code>
//     /// </example>
//     public static TResult Match<TValue, TError, TResult, TState>(this IResult<TValue, TError> result, Func<TValue, TState, TResult> onSuccess, Func<TError, TState, TResult> onError, TState state)
//         where TValue : notnull where TError : notnull =>
//         result.IsSuccess ? onSuccess(result.Value, state) : onError(result.Error, state);
//
//     #endregion
//
//     #region . async .
//
//     /// <summary>
//     /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value.
//     /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value as input.</param>
//     /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value as input.</param>
//     /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
//     /// <example>
//     /// <code>
//     /// // Process user registration with async side effects
//     /// IResult&lt;User, ValidationError&gt; registrationResult = await RegisterUserAsync(userData);
//     ///
//     /// string response = await registrationResult.MatchAsync(
//     ///     onSuccess: async user => {
//     ///         await SendWelcomeEmailAsync(user.Email).ConfigureAwait(false);
//     ///         await LogSuccessfulRegistrationAsync(user.Id).ConfigureAwait(false);
//     ///         return $"Welcome {user.Name}! Check your email for confirmation.";
//     ///     },
//     ///     onError: async error => {
//     ///         await LogValidationErrorAsync(error).ConfigureAwait(false);
//     ///         return $"Registration failed: {error.Message}";
//     ///     }
//     /// ).ConfigureAwait(false);
//     ///
//     /// return response;
//     /// </code>
//     /// </example>
//     public static ValueTask<TResult> MatchAsync<TValue, TError, TResult>(this IResult<TValue, TError> result, Func<TValue, ValueTask<TResult>> onSuccess, Func<TError, ValueTask<TResult>> onError)
//         where TValue : notnull where TError : notnull =>
//         result.IsSuccess ? onSuccess(result.Value) : onError(result.Error);
//
//     /// <summary>
//     /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
//     /// Both functions are asynchronous and return <see cref="ValueTask{TOut}"/>.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The asynchronous function to execute if the result is a success. It takes the success value and state as input.</param>
//     /// <param name="onError">The asynchronous function to execute if the result is an error. It takes the error value and state as input.</param>
//     /// <param name="state">The state to pass to the functions.</param>
//     /// <returns>A <see cref="ValueTask{TOut}"/> representing the asynchronous operation with the value returned by the executed function.</returns>
//     /// <example>
//     /// <code>
//     /// // Process file upload with logging context
//     /// IResult&lt;FileInfo, UploadError&gt; uploadResult = await UploadFileAsync(fileData);
//     /// var logger = serviceProvider.GetService&lt;ILogger&gt;();
//     ///
//     /// var response = await uploadResult.MatchAsync(
//     ///     onSuccess: async (fileInfo, log) => {
//     ///         await log.LogInformationAsync($"File uploaded: {fileInfo.Name}").ConfigureAwait(false);
//     ///         await SendFileProcessedNotificationAsync(fileInfo.Id).ConfigureAwait(false);
//     ///         return new { Success = true, FileId = fileInfo.Id };
//     ///     },
//     ///     onError: async (error, log) => {
//     ///         await log.LogErrorAsync($"Upload failed: {error.Reason}").ConfigureAwait(false);
//     ///         return new { Success = false, FileId = (string?)null };
//     ///     },
//     ///     state: logger
//     /// ).ConfigureAwait(false);
//     /// </code>
//     /// </example>
//     public static ValueTask<TResult> MatchAsync<TValue, TError, TResult, TState>(this IResult<TValue, TError> result, Func<TValue, TState, ValueTask<TResult>> onSuccess, Func<TError, TState, ValueTask<TResult>> onError, TState state)
//         where TValue : notnull where TError : notnull =>
//         result.IsSuccess ? onSuccess(result.Value, state) : onError(result.Error, state);
//
//     /// <summary>
//     /// Executes one of the two provided synchronous functions depending on whether this result is a success or an error,
//     /// returning the result wrapped in a completed <see cref="ValueTask{TResult}"/>.
//     /// This is a convenience method for scenarios where you need to match with synchronous functions but return a ValueTask.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The function to execute if the result is a success. It takes the success value as input.</param>
//     /// <param name="onError">The function to execute if the result is an error. It takes the error value as input.</param>
//     /// <returns>A completed <see cref="ValueTask{TResult}"/> containing the value returned by the executed function.</returns>
//     /// <example>
//     /// <code>
//     /// // Convert validation result to API response in async context
//     /// public async ValueTask&lt;IActionResult&gt; ValidateUserAsync(UserRequest request) {
//     ///     IResult&lt;User, ValidationError&gt; validationResult = ValidateUserData(request);
//     ///
//     ///     return await validationResult.MatchAsync(
//     ///         onSuccess: user => Ok(new { user.Id, user.Name }),
//     ///         onError: error => BadRequest(new { error.Message })
//     ///     ).ConfigureAwait(false);
//     /// }
//     /// </code>
//     /// </example>
//     public static ValueTask<TResult> MatchAsync<TValue, TError, TResult>(this IResult<TValue, TError> result, Func<TValue, TResult> onSuccess, Func<TError, TResult> onError)
//         where TValue : notnull where TError : notnull =>
//         ValueTask.FromResult(result.IsSuccess ? onSuccess(result.Value) : onError(result.Error));
//
//     /// <summary>
//     /// Asynchronously executes one of the two provided functions depending on whether this result is a success or an error, returning a new value and passing additional state.
//     /// Both functions are synchronous and the result is wrapped in a completed <see cref="ValueTask{TResult}"/>.
//     /// </summary>
//     /// <typeparam name="TValue">The type of the current success value.</typeparam>
//     /// <typeparam name="TError">The type of the error value.</typeparam>
//     /// <typeparam name="TResult">The type of the value returned by the matching functions.</typeparam>
//     /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
//     /// <param name="result">The result to pattern match.</param>
//     /// <param name="onSuccess">The synchronous function to execute if the result is a success. It takes the success value and state as input.</param>
//     /// <param name="onError">The synchronous function to execute if the result is an error. It takes the error value and state as input.</param>
//     /// <param name="state">The state to pass to the functions.</param>
//     /// <returns>A completed <see cref="ValueTask{TResult}"/> containing the value returned by the executed function.</returns>
//     /// <example>
//     /// <code>
//     /// // Process database result with transaction context in async pipeline
//     /// IResult&lt;Customer, DatabaseError&gt; customerResult = LoadCustomer(customerId);
//     /// var transaction = dbContext.Database.BeginTransaction();
//     ///
//     /// var apiResponse = await customerResult.MatchAsync(
//     ///     onSuccess: (customer, tx) => {
//     ///         tx.Commit();
//     ///         return new CustomerResponse { Id = customer.Id, Name = customer.Name };
//     ///     },
//     ///     onError: (error, tx) => {
//     ///         tx.Rollback();
//     ///         return new CustomerResponse { Error = error.Message };
//     ///     },
//     ///     state: transaction
//     /// ).ConfigureAwait(false);
//     /// </code>
//     /// </example>
//     public static ValueTask<TResult> MatchAsync<TValue, TError, TResult, TState>(this IResult<TValue, TError> result, Func<TValue, TState, TResult> onSuccess, Func<TError, TState, TResult> onError, TState state)
//         where TValue : notnull where TError : notnull =>
//         ValueTask.FromResult(result.IsSuccess ? onSuccess(result.Value, state) : onError(result.Error, state));
//
//     #endregion
// }
