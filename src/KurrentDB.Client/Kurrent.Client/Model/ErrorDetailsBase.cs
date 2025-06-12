namespace Kurrent.Client.Model;

/// <summary>
/// Represents a contract for error details that can be thrown as exceptions.
/// </summary>
public abstract record ErrorDetailsBase {
    protected ErrorDetailsBase() {
        ErrorCode = GetType().Name;
    }

    protected string ErrorCode { get; }

    protected abstract string ErrorMessage { get; }

    public KurrentClientException Throw(Exception? innerException = null) =>
        throw GetException(innerException);

    public KurrentClientException GetException(Exception? innerException = null) =>
        new(ErrorCode, ErrorMessage, innerException);
}
