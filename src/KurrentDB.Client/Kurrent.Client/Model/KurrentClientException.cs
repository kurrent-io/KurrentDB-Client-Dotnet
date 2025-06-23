// ReSharper disable InvertIf

using Google.Protobuf;
using Google.Rpc;
using Grpc.Core;
using Humanizer;

namespace Kurrent.Client.Model;

public record FieldViolation(string Field, string Description);

/// <summary>
/// Exception class used to indicate errors specific to the operation and state of the Kurrent client.
/// Provides relevant error details, including error codes, statuses, field violations, and associated metadata.
/// </summary>
[PublicAPI]
// public class KurrentClientException(string errorCode, string message, Exception? innerException = null) : Exception(message, innerException) {
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
    /// Creates and throws a <see cref="KurrentClientException"/> with an error code derived from the type name of <typeparamref name="T"/>
    /// and details extracted from the specified <paramref name="exception"/>.
    /// </summary>
    /// <typeparam name="T">The type of the protocol buffer message used for deserializing detailed error information.</typeparam>
    /// <param name="exception">The <see cref="RpcException"/> from which the error details, status code, and metadata are obtained.</param>
    /// <returns>This method always throws an exception, so it never returns a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method.</exception>
    public static KurrentClientException Throw<T>(RpcException exception) where T : class, IMessage<T>, new() {
	    var fieldViolations = new List<FieldViolation>();

	    var message = exception.Status.Detail;
	    var metadata = new Metadata();

	    var status = exception.GetRpcStatus();

	    if (status is not null) {
		    foreach (var detail in status.Details) {
			    if (!detail.TryUnpack<T>(out var unpackedDetail))
				    continue;

			    switch (unpackedDetail) {
				    case BadRequest badRequest:
					    fieldViolations.AddRange(badRequest.FieldViolations.Select(fv => new FieldViolation(fv.Field, fv.Description)));
					    break;

				    case PreconditionFailure preconditionFailure:
					    fieldViolations.AddRange(preconditionFailure.Violations.Select(v => new FieldViolation(v.Subject, v.Description)));
					    break;

				    case ErrorInfo errorInfo:
					    foreach (var kvp in errorInfo.Metadata) {
                            metadata.With(kvp.Key, kvp.Value);
                        }
					    break;

				    case ResourceInfo resourceInfo:
					    message = resourceInfo.Description;
					    metadata.With(nameof(resourceInfo.ResourceType), resourceInfo.ResourceType)
                            .With(nameof(resourceInfo.ResourceName), resourceInfo.ResourceName)
                            .With(nameof(resourceInfo.Owner), resourceInfo.Owner);
					    break;
			    }
		    }
	    }

	    if (fieldViolations.Count > 0)
		    metadata.With("FieldViolations", fieldViolations);

	    throw new KurrentClientException(
		    exception.StatusCode.ToString(),
		    message,
		    metadata,
		    exception
	    );
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
        return new("Unknown", $"Unexpected error on {operation}: {innerException.Message}", null, innerException);
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


    public static KurrentClientException CreateUnexpected(string operation, Exception innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        return new(operation.Underscore().ToUpperInvariant(), $"Unexpected behaviour detected during {operation}: {innerException.Message}", null, innerException);
    }
    public static KurrentClientException CreateUnexpected(string operation, Metadata metadata, Exception innerException) {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        return new(operation.Underscore().ToUpperInvariant(), $"Unexpected behaviour detected during {operation}: {innerException.Message}", metadata, innerException);
    }

    public static KurrentClientException ThrowUnexpected(string operation, Exception innerException) =>
	     throw CreateUnexpected(operation,innerException);

	 public static KurrentClientException ThrowUnexpected(string operation, Metadata metadata, Exception innerException) =>
	throw CreateUnexpected(operation, metadata,innerException);

}
