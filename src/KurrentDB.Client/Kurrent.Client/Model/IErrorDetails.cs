namespace Kurrent.Client.Model;

/// <summary>
/// Represents a contract for error details that can be thrown as exceptions.
/// </summary>
public interface IErrorDetails {
    public void Throw() => KurrentClientException.Throw(this);
}
