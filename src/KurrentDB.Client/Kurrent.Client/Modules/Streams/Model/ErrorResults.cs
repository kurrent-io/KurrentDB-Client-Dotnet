using Kurrent.Variant;

namespace Kurrent.Client.Streams;

[PublicAPI]
public readonly partial record struct InspectRecordError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct AppendStreamFailure : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.StreamTombstoned,
    ErrorDetails.StreamRevisionConflict,
    ErrorDetails.TransactionMaxSizeExceeded>;

[PublicAPI]
public readonly partial record struct ReadError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.StreamTombstoned>;

[PublicAPI]
public readonly partial record struct DeleteStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.StreamTombstoned,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct TombstoneError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted, // can we tombstone a deleted stream?
    ErrorDetails.StreamTombstoned,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct GetStreamInfoError : IVariantResultError<
    ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct SetStreamMetadataError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.StreamTombstoned,
    ErrorDetails.StreamRevisionConflict>;

[PublicAPI]
public readonly partial record struct TruncateStreamError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted,
    ErrorDetails.StreamTombstoned,
    ErrorDetails.StreamRevisionConflict>;



[PublicAPI]
public readonly partial record struct GetStreamMetadataError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    ErrorDetails.StreamDeleted>;
