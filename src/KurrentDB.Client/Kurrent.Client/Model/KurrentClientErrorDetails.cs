using System.Diagnostics;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents a contract for error details that can be thrown as exceptions.
/// The error code is derived from the type name, and the error message is defined in the derived class.
/// </summary>
[PublicAPI]
public abstract record KurrentClientErrorDetails {
    /// <summary>
    /// Initializes a new instance of the <see cref="KurrentClientErrorDetails"/> record.
    /// The error code is automatically set to the name of the derived type.
    /// </summary>
    protected KurrentClientErrorDetails() => ErrorCode = GetType().Name;

    /// <summary>
    /// The unique code identifying the error.
    /// <remarks>
    /// This code is automatically derived from the name of the concrete error type upon instantiation.
    /// </remarks>
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// The descriptive message for the error.
    /// <remarks>
    /// Implementers must override this property to provide specific error details.
    /// </remarks>
    /// </summary>
    public abstract string ErrorMessage { get; }

    /// <summary>
    /// Creates a <see cref="KurrentClientException"/> instance based on this error definition.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>A new <see cref="KurrentClientException"/> instance populated with details from this error definition.</returns>
    public KurrentClientException CreateException(Exception? innerException = null) {
        Debug.Assert(
            !string.IsNullOrWhiteSpace(ErrorMessage),
            $"ErrorMessage cannot be empty. If you see this, ensure that "
          + $"the property is overridden in the `{GetType().FullName}` class.");
        return new(ErrorCode, ErrorMessage, innerException);
    }

    /// <summary>
    /// Creates and throws a <see cref="KurrentClientException"/> instance based on this error definition.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <returns>This method always throws an exception and does not return a value.</returns>
    /// <exception cref="KurrentClientException">Always thrown by this method, encapsulating the error details defined in this record.</exception>
    public KurrentClientException Throw(Exception? innerException = null) =>
        throw CreateException(innerException);

    public override string ToString() => $"({ErrorCode}) {ErrorMessage}";
}
