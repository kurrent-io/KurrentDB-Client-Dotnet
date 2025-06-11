namespace Kurrent.Client.Model;

/// <summary>
/// Base exception class for all KurrentDB client exceptions.
/// </summary>
public class KurrentClientException(string errorCode, string message, Exception? innerException = null) : Exception(message, innerException) {
    public static void Throw<T>(T error, Exception? innerException = null) => throw new KurrentClientException(typeof(T).Name, error!.ToString()!, innerException);

    public static Exception Unknown(string operation, Exception innerException) =>
        new KurrentClientException("Unknown", $"Unexpected error on {operation}: {innerException.Message}", innerException);
}
