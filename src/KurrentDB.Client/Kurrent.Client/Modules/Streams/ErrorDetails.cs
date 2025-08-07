// ReSharper disable CheckNamespace

using Kurrent.Client.Legacy;
using Kurrent.Client.Streams;
using Types = KurrentDB.Protocol.Streams.V2.StreamsErrorDetails.Types;

namespace Kurrent.Client;

public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamNotFound))]
    public readonly partial record struct StreamNotFound;

    /// <summary>
    /// Represents an error indicating that the specified stream has been deleted.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamDeleted))]
    public readonly partial record struct StreamDeleted;

    /// <summary>
    /// Represents an error indicating that the specified stream has been tombstoned, meaning it is no longer available for appending.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamTombstoned))]
    public readonly partial record struct StreamTombstoned;

    /// <summary>
    /// Indicates an error where the maximum allowable size of a transaction has been exceeded.
    /// </summary>
    [KurrentOperationError(typeof(Types.TransactionMaxSizeExceeded))]
    public readonly partial record struct TransactionMaxSizeExceeded;

    /// <summary>
    /// Represents a failure due to an unexpected revision conflict during an append operation.
    /// </summary>
    [KurrentOperationError(typeof(Types.StreamRevisionConflict))]
    public readonly partial record struct StreamRevisionConflict;

    /// <summary>
    /// Represents an error indicating that the specified log position could not be found in the transaction log.
    /// </summary>
    [KurrentOperationError(typeof(Types.LogPositionNotFound))]
    public readonly partial record struct LogPositionNotFound;
}

public static partial class ErrorDetails {
    public static StreamRevisionConflict AsStreamRevisionConflictError(this Exception ex) {
        return ex.MapToResultError(
            LegacyErrorCodes.WrongExpectedVersion,
            static rex => new StreamRevisionConflict(x => x
                .With<StreamName>("Stream", rex.Trailers.GetValue("stream-name") ?? StreamName.None)
                .With("ExpectedRevision", GetStreamRevision(rex.Trailers, "expected-version"))
                .With("ActualRevision", GetStreamRevision(rex.Trailers, "actual-version"))
            )
        );

        static StreamRevision GetStreamRevision(global::Grpc.Core.Metadata metadata, string key) =>
            metadata.GetValue(key) is { } val && long.TryParse(val, out var revision)
                ? StreamRevision.From(revision) : ExpectedStreamState.NoStream;
    }
}
