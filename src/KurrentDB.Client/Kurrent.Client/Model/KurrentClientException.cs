using Google.Protobuf;
using Google.Rpc;
using Grpc.Core;

namespace Kurrent.Client.Model;

public record FieldViolation(string Field, string Description);

/// <summary>
/// Exception class used to indicate errors specific to the operation and state of the Kurrent client.
/// Provides relevant error details, including error codes, statuses, field violations, and associated metadata.
/// </summary>
[PublicAPI]
public class KurrentClientException : Exception {
	/// <summary>
	/// Gets the error code associated with this exception.
	/// </summary>
	public string ErrorCode { get; }

    /// <summary>
    /// Gets the validation errors if this exception represents validation failures.
    /// </summary>
    public IReadOnlyCollection<FieldViolation> FieldViolations { get; }

    /// <summary>
    /// Gets additional metadata associated with this exception.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public KurrentClientException(
	    string errorCode, string message, Exception? innerException = null,
	    IEnumerable<FieldViolation>? fieldViolations = null, IDictionary<string, string>? metadata = null
    ) : base(message, innerException) {
        ErrorCode = errorCode;
        FieldViolations = fieldViolations?.ToList().AsReadOnly() ?? new List<FieldViolation>().AsReadOnly();
        Metadata = metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).AsReadOnly() ?? new Dictionary<string, string>().AsReadOnly();
    }

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
	    throw new KurrentClientException(typeof(T).Name, error.ToString()!, innerException);

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

	    var code     = exception.StatusCode.ToString();
	    var message  = exception.Status.Detail;
	    var metadata = new Dictionary<string, string>();

	    var status = exception.GetRpcStatus();

	    if (status is null)
		    throw new KurrentClientException(
			    code,
			    message,
			    exception,
			    fieldViolations,
			    metadata
		    );

	    foreach (var detail in status.Details) {
		    if (detail.TryUnpack<BadRequest>(out var badRequest))
			    fieldViolations.AddRange(badRequest.FieldViolations.Select(fv => new FieldViolation(fv.Field, fv.Description)));

		    else if (detail.TryUnpack<PreconditionFailure>(out var preconditionFailure))
			    fieldViolations.AddRange(preconditionFailure.Violations.Select(v => new FieldViolation(v.Subject, v.Description)));

		    else if (detail.TryUnpack<ErrorInfo>(out var errorInfo)) {
			    code = errorInfo.Reason;
			    foreach (var kvp in errorInfo.Metadata)
				    metadata[kvp.Key] = kvp.Value;
		    }

		    else if (detail.TryUnpack<ResourceInfo>(out var resourceInfo)) {
			    if (!string.IsNullOrEmpty(resourceInfo.Description))
				    message = resourceInfo.Description;

			    metadata[nameof(resourceInfo.ResourceType)] = resourceInfo.ResourceType;
			    metadata[nameof(resourceInfo.ResourceName)] = resourceInfo.ResourceName;
			    metadata[nameof(resourceInfo.Owner)]        = resourceInfo.Owner;
		    }
	    }

	    throw new KurrentClientException(
		    code,
		    message,
		    exception,
		    fieldViolations,
		    metadata
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
        return new("Unknown", $"Unexpected error on {operation}: {innerException.Message}", innerException);
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

public static class KurrentClientExceptionExtensions {
	// public static Exception ToException(this IVariantResultError variantError, Exception? innerException = null) {
	//     if (variantError..Value is IResultError error)
	//         return error.CreateException(innerException);
	//
	//     var invalidEx = new InvalidOperationException(
	//         $"The error value is not a KurrentClientErrorDetails instance but rather " +
	//         $"{variantError.Value.GetType().FullName}", innerException);
	//
	//     return KurrentClientException.CreateUnknown("KurrentClientExceptionExtensions.Throw", invalidEx);
	// }
	//
	// public static Exception Throw(this IVariant variantError, Exception? innerException = null) =>
	//     throw variantError.ToException(innerException);
	//
	// public static Exception ToException(this IVariantResultError variantError, Exception? innerException = null) =>
	//     variantError.CreateException(innerException);
	//
	// public static Exception Throw(this IVariantResultError variantError, Exception? innerException = null) =>
	//     throw variantError.Throw(innerException);
}
