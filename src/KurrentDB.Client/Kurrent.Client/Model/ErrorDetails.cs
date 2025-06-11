namespace Kurrent.Client.Model;

/// <summary>
/// Provides a set of error detail types for representing specific append operation failures in a stream.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    /// <param name="Stream">The name of the stream that could not be located.</param>
    public readonly record struct StreamNotFound(string Stream) : IErrorDetails {
        public override string ToString() => $"Stream '{Stream}' not found.";
    }

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    /// <param name="Stream">The name of the stream that has been deleted.</param>
    public readonly record struct StreamDeleted(string Stream) : IErrorDetails {
        public override string ToString() => $"Stream '{Stream}' has been deleted.";
    }

    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    public readonly struct AccessDenied(string Stream) : IErrorDetails {
        public override string ToString() => $"Stream '{Stream}' access denied.";
    }

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    /// <param name="MaxSize">The maximum allowed size of the transaction.</param>
    public readonly record struct TransactionMaxSizeExceeded(uint MaxSize) : IErrorDetails {
        public override string ToString() => $"Transaction size exceeded. Maximum allowed size: {MaxSize}.";
    }

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    /// <param name="StreamRevision">The actual revision of the stream.</param>
    public readonly record struct StreamRevisionConflict(string Stream, StreamRevision StreamRevision) : IErrorDetails {
        public override string ToString() => $"Stream '{Stream}' operation failed due to revision conflict. Actual revision: {StreamRevision}.";
    }

    // public readonly record struct WrongExpectedRevision(string Stream, ExpectedStreamState ExpectedStreamState, StreamRevision StreamRevision) {
    //     public override string ToString() => $"Stream '{Stream}' operation failed due to revision conflict. Expected revision: {ExpectedStreamState}, actual revision: {StreamRevision}.";
    // }
}
