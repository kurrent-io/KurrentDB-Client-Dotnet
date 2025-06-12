using System.Diagnostics.CodeAnalysis;

namespace Kurrent;

public partial class Result<TSuccess, TError> {
    /// <summary>
    /// Attempts to get the success value.
    /// </summary>
    /// <param name="success">When this method returns, contains the success value if the operation was successful;
    /// otherwise, the default value for <typeparamref name="TSuccess"/>.</param>
    /// <returns>
    /// <c>true</c> if the operation was successful and <paramref name="success"/> contains the success value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetValue([NotNullWhen(true)] out TSuccess? success) {
        success = IsSuccess ? _success : default;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to get the error value.
    /// </summary>
    /// <param name="error">When this method returns, contains the error value if the operation failed;
    /// otherwise, the default value for <typeparamref name="TError"/>.</param>
    /// <returns>
    /// <c>true</c> if the operation failed and <paramref name="error"/> contains the error value;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool TryGetError([NotNullWhen(false)] out TError? error) {
        error = IsError ? _error : default;
        return IsError;
    }
}
