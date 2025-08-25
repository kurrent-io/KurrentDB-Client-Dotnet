// ReSharper disable CheckNamespace

using Grpc.Core;
using Kurrent.Client.Streams;
using Types = KurrentDB.Protocol.Streams.V2.StreamsErrorDetails.Types;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamDeleted))]
    public partial record StreamDeleted {
        public static StreamDeleted WithDetails(StreamName stream) =>
            new($"The specified stream '{stream}' has been deleted.", Metadata.New.With("StreamName", stream));
    }

    /// <summary>
    /// Represents an error indicating that the specified stream has been tombstoned, meaning it is no longer available for appending.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamTombstoned))]
    public partial record StreamTombstoned {
        public static StreamTombstoned WithDetails(StreamName stream) =>
            new($"The specified stream '{stream}' has been tombstoned and is no longer available for appending.",
                Metadata.New.With("StreamName", stream));
    }

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    [KurrentOperationError(typeof(Types.TransactionMaxSizeExceeded))]
    public partial record TransactionMaxSizeExceeded {
        public static TransactionMaxSizeExceeded WithDetails(int maxSize) =>
            new($"The append transaction size exceeds the maximum allowed size of {maxSize} bytes.",
                Metadata.New.With("MaxSize", maxSize));
    }

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during a stream operation.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamRevisionConflict))]
    public partial record StreamRevisionConflict {
        public static StreamRevisionConflict WithDetails(string operation, RpcException rex) {
            StreamName     stream           = rex.Trailers.GetValue("stream-name")!;
            StreamRevision expectedRevision = long.Parse(rex.Trailers.GetValue("expected-version")!);
            StreamRevision actualRevision   = long.Parse(rex.Trailers.GetValue("actual-version")!);

            var errorData = rex.ExtractErrorData()
                .With("Operation", operation)
                .With("Stream", stream)
                .With("ExpectedRevision", expectedRevision)
                .With("ActualRevision", actualRevision);

            var errorMessage = $"The operation '{operation}' on stream '{stream}' failed due to a revision conflict. " +
                               $"(expected: {expectedRevision}, actual: {actualRevision})";

            return new(errorMessage, errorData);
        }
    }

    /// <summary>
    /// Represents an error indicating that the specified log position could not be found in the transaction log.
    /// </summary>
    [KurrentOperationError(typeof(Types.LogPositionNotFound))]
    public partial record LogPositionNotFound {
        public static StreamRevisionConflict WithDetails(LogPosition position) =>
            new($"The specified log position '{position}' could not be found in the transaction log.",
                Metadata.New.With("LogPosition", position));
    }
}
