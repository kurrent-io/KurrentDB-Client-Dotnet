using System.Runtime.ExceptionServices;

namespace Kurrent;

public static partial class Result {
    /// <summary>
    /// Attempts to execute the specified function and returns a result indicating success or failure.
    /// If the function throws an exception, it is caught and transformed into an error using the provided error handler.
    /// </summary>
    /// <typeparam name="TValue">The type of the successful result.</typeparam>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The function to execute, returning a result of type <typeparamref name="TValue"/>.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed function.</returns>
    /// <example>
    /// <code>
    /// // Convert file parsing from exceptions to Result pattern
    /// Result&lt;ConfigData, string&gt; configResult = Result.Try(
    ///     operation: () => {
    ///         var json = File.ReadAllText("config.json");
    ///         return JsonSerializer.Deserialize&lt;ConfigData&gt;(json);
    ///     },
    ///     onError: ex => ex switch {
    ///         FileNotFoundException => "Configuration file not found",
    ///         JsonException => "Invalid JSON format in configuration",
    ///         UnauthorizedAccessException => "Access denied to configuration file",
    ///         _ => $"Failed to load configuration: {ex.Message}"
    ///     }
    /// );
    /// 
    /// // Now you can use Result methods instead of try/catch
    /// var appConfig = configResult.Match(
    ///     onSuccess: config => $"Loaded config for {config.AppName}",
    ///     onError: error => $"Configuration error: {error}"
    /// );
    /// </code>
    /// </example>
    public static Result<TValue, TError> Try<TValue, TError>(Func<TValue> operation, Func<Exception, TError> onError) where TValue : notnull where TError : notnull {
        try {
            return operation();
        }
        catch (Exception ex) {
            return onError(ex);
        }
    }

    /// <summary>
    /// Attempts to execute the specified function and returns a result indicating success or failure.
    /// If the function throws an exception, it is caught and returned as the error value.
    /// </summary>
    /// <typeparam name="TValue">The type of the successful result.</typeparam>
    /// <param name="operation">The function to execute, returning a result of type <typeparamref name="TValue"/>.</param>
    /// <returns>A result object representing either the success value or the exception that occurred.</returns>
    /// <example>
    /// <code>
    /// // Simple exception-to-Result conversion
    /// Result&lt;int, Exception&gt; parseResult = Result.Try(() => int.Parse(userInput));
    /// 
    /// string message = parseResult.Match(
    ///     onSuccess: value => $"Parsed number: {value}",
    ///     onError: ex => $"Parse failed: {ex.Message}"
    /// );
    /// 
    /// // Can be chained with other Result operations
    /// var processedResult = parseResult
    ///     .Map(number => number * 2)
    ///     .Then(doubled => doubled > 100 
    ///         ? Result.Success&lt;string, Exception&gt;($"Large number: {doubled}")
    ///         : Result.Success&lt;string, Exception&gt;($"Small number: {doubled}"));
    /// </code>
    /// </example>
    public static Result<TValue, Exception> Try<TValue>(Func<TValue> operation) where TValue : notnull {
        try {
            return operation();
        }
        catch (Exception ex) {
            return ex;
        }
    }

    /// <summary>
    /// Attempts to execute the provided action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The action to execute.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    /// <example>
    /// <code>
    /// // Convert file operations from exceptions to Result pattern
    /// Result&lt;Void, string&gt; writeResult = Result.Try(
    ///     operation: () => {
    ///         Directory.CreateDirectory("logs");
    ///         File.WriteAllText("logs/app.log", logData);
    ///     },
    ///     onError: ex => ex switch {
    ///         UnauthorizedAccessException => "Permission denied to write log file",
    ///         DirectoryNotFoundException => "Log directory path is invalid",
    ///         IOException => "Failed to write to log file - disk may be full",
    ///         _ => $"Unexpected error writing log: {ex.Message}"
    ///     }
    /// );
    /// 
    /// // Handle the result without exceptions
    /// writeResult.Switch(
    ///     onSuccess: _ => Console.WriteLine("Log written successfully"),
    ///     onError: error => Console.WriteLine($"Logging failed: {error}")
    /// );
    /// </code>
    /// </example>
    public static Result<Void, TError> Try<TError>(Action operation, Func<Exception, TError> onError) where TError : notnull {
        try {
            operation();
            return Void.Value;
        }
        catch (Exception ex) {
            return onError(ex);
        }
    }

    /// <summary>
    /// Attempts to execute the provided action and returns a result indicating success or failure.
    /// If the action throws an exception, it is caught and returned as the error value.
    /// </summary>
    /// <param name="operation">The action to execute.</param>
    /// <returns>A result object representing either success (Void) or the exception that occurred.</returns>
    /// <example>
    /// <code>
    /// // Convert simple operations to Result pattern
    /// Result&lt;Void, Exception&gt; deleteResult = Result.Try(() => File.Delete(tempFilePath));
    /// 
    /// var message = deleteResult.Match(
    ///     onSuccess: _ => "File deleted successfully",
    ///     onError: ex => $"Failed to delete file: {ex.Message}"
    /// );
    /// 
    /// // Chain with other operations
    /// var cleanupResult = deleteResult
    ///     .Then(_ => Result.Try(() => Directory.Delete(tempDirectory, true)))
    ///     .Match(
    ///         onSuccess: _ => "Cleanup completed",
    ///         onError: ex => $"Cleanup failed: {ex.Message}"
    ///     );
    /// </code>
    /// </example>
    public static Result<Void, Exception> Try(Action operation) {
        try {
            operation();
            return Void.Value;
        }
        catch (Exception ex) {
            return ex;
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous function and returns a <see cref="Result{TSuccess, TError}"/>
    /// indicating whether the function executed successfully or encountered an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value returned on successful execution.</typeparam>
    /// <typeparam name="TError">The type of the error returned if execution fails.</typeparam>
    /// <param name="operation">The asynchronous function to execute whose result will be captured if successful.</param>
    /// <param name="onError">A function that accepts an exception and transforms it into an error object of type <typeparamref name="TError"/>.</param>
    /// <returns>
    /// A <see cref="Result{TSuccess, TError}"/> instance representing the outcome of the function execution.
    /// If the operation succeeds, the result contains the success value.
    /// If the operation fails, the result contains the mapped error.
    /// </returns>
    /// <example>
    /// <code>
    /// // Convert async API calls from exceptions to Result pattern
    /// Result&lt;UserProfile, ApiError&gt; profileResult = await Result.TryAsync(
    ///     operation: async () => {
    ///         using var client = new HttpClient();
    ///         var response = await client.GetAsync($"api/users/{userId}").ConfigureAwait(false);
    ///         response.EnsureSuccessStatusCode();
    ///         var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    ///         return JsonSerializer.Deserialize&lt;UserProfile&gt;(json);
    ///     },
    ///     onError: ex => ex switch {
    ///         HttpRequestException httpEx => new ApiError("Network error", httpEx.Message),
    ///         TaskCanceledException => new ApiError("Timeout", "Request timed out"),
    ///         JsonException jsonEx => new ApiError("Parse error", "Invalid response format"),
    ///         _ => new ApiError("Unknown", ex.Message)
    ///     }
    /// ).ConfigureAwait(false);
    /// 
    /// // Use async Result patterns for further processing
    /// var welcomeMessage = await profileResult.MatchAsync(
    ///     onSuccess: async profile => await GenerateWelcomeAsync(profile).ConfigureAwait(false),
    ///     onError: async error => await LogErrorAsync(error).ConfigureAwait(false)
    /// ).ConfigureAwait(false);
    /// </code>
    /// </example>
    public static async ValueTask<Result<TValue, TError>> TryAsync<TValue, TError>(Func<ValueTask<TValue>> operation, Func<Exception, TError> onError) where TValue : notnull where TError : notnull {
        try {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex) {
            return onError(ex);
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous function and returns a result indicating success or failure.
    /// If the function throws an exception, it is caught and returned as the error value.
    /// </summary>
    /// <typeparam name="TValue">The type of the success value returned on successful execution.</typeparam>
    /// <param name="operation">The asynchronous function to execute whose result will be captured if successful.</param>
    /// <returns>A result object representing either the success value or the exception that occurred.</returns>
    /// <example>
    /// <code>
    /// // Simple async exception-to-Result conversion
    /// Result&lt;string, Exception&gt; fileResult = await Result.TryAsync(async () => {
    ///     return await File.ReadAllTextAsync("data.txt").ConfigureAwait(false);
    /// }).ConfigureAwait(false);
    /// 
    /// // Chain with other async operations
    /// var processedResult = await fileResult
    ///     .MapAsync(async content => {
    ///         var lines = content.Split('\n');
    ///         return await ProcessLinesAsync(lines).ConfigureAwait(false);
    ///     })
    ///     .ConfigureAwait(false);
    /// 
    /// // Handle the final result
    /// processedResult.Switch(
    ///     onSuccess: data => Console.WriteLine($"Processed {data.Count} items"),
    ///     onError: ex => Console.WriteLine($"Processing failed: {ex.Message}")
    /// );
    /// </code>
    /// </example>
    public static async ValueTask<Result<TValue, Exception>> TryAsync<TValue>(Func<ValueTask<TValue>> operation) where TValue : notnull {
        try {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex) {
            return ex;
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous action and returns a result indicating success or failure.
    /// </summary>
    /// <typeparam name="TError">The type of the error result.</typeparam>
    /// <param name="operation">The asynchronous action to execute.</param>
    /// <param name="onError">A function to handle any exception thrown by <paramref name="operation"/>, returning an error of type <typeparamref name="TError"/>.</param>
    /// <returns>A result object representing either the success or the error of the executed action.</returns>
    /// <example>
    /// <code>
    /// // Convert async void operations to Result pattern
    /// Result&lt;Void, DatabaseError&gt; saveResult = await Result.TryAsync(
    ///     operation: async () => {
    ///         using var context = new AppDbContext();
    ///         context.Users.Add(newUser);
    ///         await context.SaveChangesAsync().ConfigureAwait(false);
    ///     },
    ///     onError: ex => ex switch {
    ///         DbUpdateException dbEx => new DatabaseError("Constraint violation", dbEx.Message),
    ///         TimeoutException => new DatabaseError("Timeout", "Database operation timed out"),
    ///         _ => new DatabaseError("Unknown", $"Database error: {ex.Message}")
    ///     }
    /// ).ConfigureAwait(false);
    /// 
    /// // Chain with notification
    /// await saveResult
    ///     .OnSuccessAsync(async _ => await SendUserWelcomeEmailAsync(newUser.Email).ConfigureAwait(false))
    ///     .OnFailureAsync(async error => await LogDatabaseErrorAsync(error).ConfigureAwait(false))
    ///     .ConfigureAwait(false);
    /// </code>
    /// </example>
    public static async ValueTask<Result<Void, TError>> TryAsync<TError>(Func<ValueTask> operation, Func<Exception, TError> onError) where TError : notnull {
        try {
            await operation().ConfigureAwait(false);
            return Void.Value;
        }
        catch (Exception ex) {
            return onError(ex);
        }
    }

    /// <summary>
    /// Attempts to execute the provided asynchronous action and returns a result indicating success or failure.
    /// If the action throws an exception, it is caught and returned as the error value.
    /// </summary>
    /// <param name="operation">The asynchronous action to execute.</param>
    /// <returns>A result object representing either success (Void) or the exception that occurred.</returns>
    /// <example>
    /// <code>
    /// // Simple async action-to-Result conversion
    /// Result&lt;Void, Exception&gt; uploadResult = await Result.TryAsync(async () => {
    ///     await UploadFileAsync(fileData, "backup.zip").ConfigureAwait(false);
    /// }).ConfigureAwait(false);
    /// 
    /// // Chain cleanup operations
    /// var cleanupResult = await uploadResult
    ///     .ThenAsync(async _ => {
    ///         // Only run cleanup if upload succeeded
    ///         await DeleteTempFilesAsync().ConfigureAwait(false);
    ///         return Void.Value;
    ///     })
    ///     .ConfigureAwait(false);
    /// 
    /// var summary = cleanupResult.Match(
    ///     onSuccess: _ => "Upload and cleanup completed successfully",
    ///     onError: ex => $"Operation failed: {ex.Message}"
    /// );
    /// </code>
    /// </example>
    public static async ValueTask<Result<Void, Exception>> TryAsync(Func<ValueTask> operation) {
        try {
            await operation().ConfigureAwait(false);
            return Void.Value;
        }
        catch (Exception ex) {
            return ex;
        }
    }
}
