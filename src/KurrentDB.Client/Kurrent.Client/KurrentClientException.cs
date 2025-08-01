// ReSharper disable InvertIf

using System.Globalization;
using Google.Rpc;
using Grpc.Core;

namespace Kurrent.Client;

/// <summary>
/// Exception class used to indicate errors specific to the operation and state of the Kurrent client.
/// Provides relevant error details, including error codes, statuses, field violations, and associated metadata.
/// </summary>
[PublicAPI]
public class KurrentClientException(string errorCode, string message, Metadata? metadata = null, Exception? innerException = null) : Exception(message, innerException) {
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; } = errorCode;

    /// <summary>
    /// Additional context about the error.
    /// </summary>
    public Metadata Metadata { get; } = metadata is not null ? new(metadata) : [];

    public static KurrentClientException Wrap<T>(T exception, string? errorCode = null) where T : Exception =>
        new KurrentClientException(errorCode ?? exception.GetType().Name, exception.Message, null, exception);

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> with an error code derived from the type name of <typeparamref name="T"/>
    /// and a message from the string representation of the <paramref name="error"/> object.
    /// </summary>
    /// <typeparam name="T">The type of the error object. The name of this type will be used as the error code.</typeparam>
    /// <param name="error">The error object. Its string representation will be used as the exception message.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    public static KurrentClientException Throw<T>(T error, Exception? innerException = null) where T : notnull =>
        throw new KurrentClientException(typeof(T).Name, error.ToString()!, null, innerException);

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> with comprehensive error details
    /// extracted from the specified <paramref name="exception"/>.
    /// Supports database-relevant Google.Rpc error detail types and provides semantic error code mapping.
    /// </summary>
    /// <param name="exception">The <see cref="RpcException"/> from which the error details, status code, and metadata are obtained.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    public static KurrentClientException Throw(RpcException exception) {
	    var status         = exception.GetRpcStatus();
	    var errorDetails   = status?.UnpackDetailMessages().ToList() ?? [];
	    var handlerResults = new List<DetailHandlerResult>();


	    // if (exception.Trailers.Get(Exceptions.ExceptionKey) is global::Grpc.Core.Metadata.Entry {} entry && entry.Key == "") {
		   //  createdException = factory.Invoke(exception);
		   //  return true;
	    // }

	    // Process each detail with clear foreach + switch for database-relevant types
	    foreach (var detail in errorDetails) {
		    var result = detail switch {
			    BadRequest br          => HandleBadRequest(br),
			    PreconditionFailure pf => HandlePreconditionFailure(pf),
			    ErrorInfo ei           => HandleErrorInfo(ei),
			    RetryInfo ri           => HandleRetryInfo(ri),
			    DebugInfo di           => HandleDebugInfo(di),
			    _                      => DetailHandlerResult.Empty // Unknown detail type - ignore
		    };

		    handlerResults.Add(result);
	    }

	    // Aggregate results functionally
	    var fieldViolations = handlerResults.SelectMany(r => r.FieldViolations).ToList();
	    var metadata = AggregateMetadata(handlerResults.SelectMany(r => r.MetadataEntries));

	    if (fieldViolations.Count > 0)
		    metadata.With("FieldViolations", fieldViolations);

	    var errorCode = MapToSemanticErrorCode(exception.StatusCode, fieldViolations);

	    throw new KurrentClientException(errorCode, exception.Status.Detail, metadata, exception);

	    // Self-contained handlers for the 5 database-relevant types
	    static DetailHandlerResult HandleBadRequest(BadRequest request) => new() {
		    FieldViolations = request.FieldViolations
			    .Select(fv => new FieldViolation(fv.Field, fv.Description))
			    .ToList()
	    };

	    static DetailHandlerResult HandlePreconditionFailure(PreconditionFailure failure) => new() {
		    FieldViolations = failure.Violations
			    .Select(v => new FieldViolation(v.Subject, v.Description))
			    .ToList()
	    };

	    static DetailHandlerResult HandleErrorInfo(ErrorInfo info) => new() {
		    MetadataEntries = info.Metadata
			    .Select(kvp => new MetadataEntry(kvp.Key, kvp.Value))
			    .Concat([
				    new("ErrorInfo.Reason", info.Reason),
				    new("ErrorInfo.Domain", info.Domain)
			    ])
			    .Where(entry => !string.IsNullOrEmpty(entry.Value))
			    .ToList()
	    };

	    static DetailHandlerResult HandleRetryInfo(RetryInfo info) => new() {
		    MetadataEntries = [
			    new("RetryInfo.RetryDelayMs",
				    (info.RetryDelay?.ToTimeSpan().TotalMilliseconds ?? 0).ToString(CultureInfo.InvariantCulture))
		    ]
	    };

	    static DetailHandlerResult HandleDebugInfo(DebugInfo info) => new() {
		    MetadataEntries = CreateDebugMetadata(info)
	    };

	    static List<MetadataEntry> CreateDebugMetadata(DebugInfo info) {
		    var entries = new List<MetadataEntry> { new("DebugInfo.Detail", info.Detail) };
		    if (info.StackEntries.Count > 0)
			    entries.Add(new("DebugInfo.StackEntries", string.Join("\n", info.StackEntries)));
		    return entries;
	    }

	    // Utility functions
	    static Metadata AggregateMetadata(IEnumerable<MetadataEntry> entries) {
		    var metadata = new Metadata();
		    foreach (var (key, value) in entries) metadata.With(key, value);
		    return metadata;
	    }
    }

    /// <summary>
    /// Represents the result of processing a single error detail type.
    /// Used for functional composition of error handling results.
    /// </summary>
    record DetailHandlerResult {
	    public List<FieldViolation> FieldViolations { get; init; } = [];
	    public List<MetadataEntry>  MetadataEntries { get; init; } = [];

        public static DetailHandlerResult Empty => new();
    }

    /// <summary>
    /// Represents a key-value pair for metadata storage.
    /// </summary>
    record MetadataEntry(string Key, string Value);

    /// <summary>
    /// Maps gRPC status codes to semantic KurrentDB error codes based on context from field violations.
    /// </summary>
    static string MapToSemanticErrorCode(StatusCode statusCode, List<FieldViolation> fieldViolations) {
	    // Check for KurrentDB-specific error patterns in field violations
	    var streamContext = fieldViolations.FirstOrDefault(fv => fv.Field.Contains("stream", StringComparison.OrdinalIgnoreCase));
	    var revisionContext = fieldViolations.Any(fv =>
		    fv.Field.Contains("revision", StringComparison.OrdinalIgnoreCase) ||
		    fv.Field.Contains("version", StringComparison.OrdinalIgnoreCase));

	    return statusCode switch {
		    StatusCode.NotFound when streamContext is not null           => "STREAM_NOT_FOUND",
		    StatusCode.PermissionDenied when streamContext is not null   => "STREAM_ACCESS_DENIED",
		    StatusCode.PermissionDenied                                  => "ACCESS_DENIED",
		    StatusCode.FailedPrecondition when revisionContext           => "STREAM_REVISION_CONFLICT",
		    StatusCode.FailedPrecondition when streamContext is not null => "STREAM_STATE_CONFLICT",
		    StatusCode.AlreadyExists when streamContext is not null      => "STREAM_ALREADY_EXISTS",
		    StatusCode.ResourceExhausted                                 => "QUOTA_EXCEEDED",
		    StatusCode.InvalidArgument when fieldViolations.Count > 0    => "INVALID_REQUEST",
		    StatusCode.InvalidArgument                                   => "INVALID_ARGUMENT",
		    StatusCode.Unauthenticated                                   => "AUTHENTICATION_REQUIRED",
		    StatusCode.Unavailable                                       => "SERVICE_UNAVAILABLE",
		    StatusCode.DeadlineExceeded                                  => "OPERATION_TIMEOUT",
		    StatusCode.Cancelled                                         => "OPERATION_CANCELLED",
		    StatusCode.Internal                                          => "INTERNAL_ERROR",
		    _                                                            => statusCode.ToString().ToUpperInvariant()
	    };
    }

    static class Exceptions {
	    public const string ExceptionKey = "exception";

	    public const string AccessDenied                    = "access-denied";
	    public const string InvalidTransaction              = "invalid-transaction";
	    public const string StreamDeleted                   = "stream-deleted";
	    public const string WrongExpectedVersion            = "wrong-expected-version";
	    public const string StreamNotFound                  = "stream-not-found";
	    public const string MaximumAppendSizeExceeded       = "maximum-append-size-exceeded";
	    public const string MissingRequiredMetadataProperty = "missing-required-metadata-property";
    }

    /// <summary>
    /// Creates a <see cref="KurrentClientException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>A new instance of <see cref="KurrentClientException"/> representing the unknown error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentClientException CreateUnknown(string operation, Exception innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        return new("Unknown", $"Unexpected behaviour detected during {operation}: {innerException.Message}", null, innerException);
    }

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> for an unknown or unexpected error that occurred during a specific operation.
    /// </summary>
    /// <param name="operation">The name of the operation during which the error occurred. Cannot be null or whitespace.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="operation"/> is empty or consists only of white-space characters.</exception>
    public static KurrentClientException ThrowUnknown(string operation, Exception innerException) =>
        throw CreateUnknown(operation, innerException);
}

/// <summary>
/// Represents a violation of a field in a request, typically used to indicate validation errors.
/// </summary>
/// <param name="Field">
/// The name of the field that violated validation rules.
/// </param>
/// <param name="Description">
/// A description of the violation, providing details about why the field is invalid or what rule was violated.
/// </param>
public record FieldViolation(string Field, string Description);
