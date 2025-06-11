namespace Kurrent.Client.Model;

/// <summary>
/// Represents a contract for error details that can be thrown as exceptions.
/// </summary>
public abstract record ErrorDetailsBase {
    public void Throw() => KurrentClientException.Throw(this);
}
