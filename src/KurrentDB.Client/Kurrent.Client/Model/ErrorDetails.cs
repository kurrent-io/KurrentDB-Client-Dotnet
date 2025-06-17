namespace Kurrent.Client.Model;

/// <summary>
/// Provides a set of error detail types for representing specific append operation failures when interacting with KurrentDB.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    /// <param name="Stream">The name of the stream that could not be located.</param>
    public readonly record struct StreamNotFound(string Stream) : IKurrentClientError {
        /// <inheritdoc />
        public string ErrorCode => nameof(StreamNotFound);
        /// <inheritdoc />
        public string ErrorMessage => $"Stream '{Stream}' not found.";
    }

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    /// <param name="Stream">The name of the stream that has been deleted.</param>
    public readonly record struct StreamDeleted(string Stream) : IKurrentClientError {
        /// <summary>
        /// Gets the unique code identifying the error.
        /// </summary>
        public string ErrorCode => nameof(StreamDeleted);

        /// <summary>
        /// Gets the error message indicating that the stream has been deleted.
        /// </summary>
        public string ErrorMessage => $"Stream '{Stream}' has been deleted.";
    }

    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    /// <param name="Stream">The name of the stream for which access was denied.</param>
    public readonly record struct AccessDenied(string Stream) : IKurrentClientError {
        /// <summary>
        /// Gets the unique code identifying the error.
        /// </summary>
        public string ErrorCode => nameof(AccessDenied);

        /// <summary>
        /// Gets the error message indicating that access to the stream was denied.
        /// </summary>
        public string ErrorMessage => $"Stream '{Stream}' access denied.";
    }

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    /// <param name="MaxSize">The maximum allowed size of the transaction.</param>
    public readonly record struct TransactionMaxSizeExceeded(uint MaxSize) : IKurrentClientError {
        /// <summary>
        /// Gets the unique code identifying the error.
        /// </summary>
        public string ErrorCode => nameof(TransactionMaxSizeExceeded);

        /// <summary>
        /// Gets the error message indicating that the transaction size limit was exceeded.
        /// </summary>
        public string ErrorMessage => $"Transaction size exceeded. Maximum allowed size: {MaxSize}";
    }

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    /// <param name="Stream">The name of the stream where the revision conflict occurred.</param>
    /// <param name="StreamRevision">The actual revision of the stream.</param>
    public readonly record struct StreamRevisionConflict(string Stream, StreamRevision StreamRevision) : IKurrentClientError {
        /// <summary>
        /// Gets the unique code identifying the error.
        /// </summary>
        public string ErrorCode => nameof(StreamRevisionConflict);

        /// <summary>
        /// Gets the error message indicating a stream revision conflict.
        /// </summary>
        public string ErrorMessage => $"Stream '{Stream}' operation failed due to revision conflict. Actual revision: {StreamRevision}.";
    }
}
