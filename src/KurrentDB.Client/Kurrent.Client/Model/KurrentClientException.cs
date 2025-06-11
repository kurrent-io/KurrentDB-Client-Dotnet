namespace Kurrent.Client.Model;

/// <summary>
/// Base exception class for all KurrentDB client exceptions.
/// </summary>
public class KurrentClientException(string errorCode, string message, Exception? innerException = null) : Exception(message, innerException) {
    public static void Throw<T>(T error, Exception? innerException = null) => throw new KurrentClientException(typeof(T).Name, error!.ToString()!, innerException);
}
