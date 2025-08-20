#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

using System.Diagnostics;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using KurrentDB.Protocol.Streams.V1;
using static Kurrent.Client.ErrorDetails;
using static Kurrent.Client.Streams.StreamsClientV1Mapper;

namespace Kurrent.Client.Streams;

static class LegacyServiceClientExtensions {
    public static async ValueTask<ReadResp?> ReadFirstOrDefault(this KurrentDB.Protocol.Streams.V1.Streams.StreamsClient client, ReadReq request, CancellationToken cancellationToken = default) {
        using var session = client.Read(request, cancellationToken: cancellationToken);
        var isEmpty = !await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
        return isEmpty || session.ResponseStream.Current == null ? null : session.ResponseStream.Current;
    }
}

public partial class StreamsClient {
    public async ValueTask<Result<StreamInfo, GetStreamInfoError>> GetStreamInfo(StreamName stream, CancellationToken cancellationToken = default) {
        // This operation can be greatly optimized by implementing it in the server
        // and as unbelievable as it sounds, the code bellow is the simplest most effective
        // way to get the stream details for the time being.
        // Even with all the permission issues, since the operation should always work...
        // This is just code that is easy to be deleted. That's it, and that's all.

        var metadataRecord = Record.None;

        try {
            var metaStream = SystemStreams.MetastreamOf(stream);
            var request    = Requests.CreateReadStreamEdgeRequest(metaStream, ReadDirection.Forwards);

            var response = await LegacyServiceClient
                .ReadFirstOrDefault(request, cancellationToken)
                .ConfigureAwait(false);

            // if the meta-stream is not found, then the stream itself does not exist either
            if (response?.ContentCase is ReadResp.ContentOneofCase.Event)
                metadataRecord = await response.Event
                    .MapToRecord(SerializerProvider, MetadataDecoder, skipDecoding: false, cancellationToken)
                    .ConfigureAwait(false);

            // // if the meta-stream is not found, then the stream itself does not exist either
            // if (response is null || response.ContentCase is ReadResp.ContentOneofCase.StreamNotFound)
            //     metadataRecord = Record.None;
            // else
            //     metadataRecord = await response.Event
            //         .MapToRecord(SerializerProvider, MetadataDecoder, skipDecoding: false, cancellationToken)
            //         .ConfigureAwait(false);
        }
        catch (RpcException rex) when (rex.StatusCode is StatusCode.FailedPrecondition && rex.Status.Detail.Contains("is deleted")) {
            return new StreamInfo { State = StreamState.Tombstoned };
        }
        catch (RpcException rex) {
            return Result.Failure<StreamInfo, GetStreamInfoError>(rex.StatusCode switch {
                // no permissions to access metadata... sigh...
                StatusCode.PermissionDenied => new AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }

        var details = StreamInfo.None;

        if (metadataRecord != Record.None) {
            var metadata = (StreamMetadata)metadataRecord.Value!;
            details = new StreamInfo {
                Metadata         = metadata,
                MetadataRevision = metadataRecord.StreamRevision,
                State            = metadata.TruncateBefore == StreamRevision.Max ? StreamState.Deleted : StreamState.Missing
            };
        }

        // at this point if it is not deleted, we have to try to read the stream itself
        // because it might not exist yet.
        if (details.State is StreamState.Deleted)
            return details;

        // now we enrich the stream info with the latest position and revision
        try {
            var request = Requests.CreateReadStreamEdgeRequest(stream, ReadDirection.Backwards);

            var response = await LegacyServiceClient
                .ReadFirstOrDefault(request, cancellationToken)
                .ConfigureAwait(false);

            if (response is null)
                return details with { State = StreamState.Active };

            // if the stream is not found, we return the details and call it a day
            if (response.ContentCase is ReadResp.ContentOneofCase.StreamNotFound)
                return details;

            if (response.ContentCase is not ReadResp.ContentOneofCase.Event)
                throw new UnreachableException(
                    $"Unexpected content case: {response.ContentCase} " +
                    $"while reading the last record of the stream {stream}"
                );

            var record = await response.Event
                .MapToRecord(SerializerProvider, MetadataDecoder, skipDecoding: true, cancellationToken)
                .ConfigureAwait(false);

            return details with {
                State                = StreamState.Active,
                LastStreamPosition   = record.Position,
                LastStreamRevision   = record.StreamRevision,
                LastStreamAppendTime = record.Timestamp
            };
        }
        catch (RpcException rex) {
            // no permissions to access the stream itself... sigh...
            // instead of returning what we have cause its ambiguous,
            // we return access denied and invalidate the operation
            return Result.Failure<StreamInfo, GetStreamInfoError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    // public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
    //     var request = Requests.CreateDeleteRequest(stream, revision);
    //
    //     try {
    //         var resp = await LegacyServiceClient
    //             .DeleteAsync(request, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
    //     }
    //     catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
    //         // the request must be done with StreamExists expectation,
    //         // if the actual version is -1, it means the stream was not found
    //         // this was a tribal knowledge discovery in the original codebase
    //         // that for whatever reason I 100% think it is a bug in the server
    //
    //         ExpectedStreamState actualState = long.Parse(rex.Trailers.GetValue("actual-version")!);
    //
    //         return actualState == ExpectedStreamState.NoStream
    //             ? Result.Failure<LogPosition, DeleteStreamError>(new NotFound())
    //             : Result.Failure<LogPosition, DeleteStreamError>(new StreamRevisionConflict(x => x
    //                 .With<StreamName>("Stream", rex.Trailers.GetValue("stream-name") ?? StreamName.None)
    //                 .With<StreamRevision>("ExpectedRevision", long.Parse(rex.Trailers.GetValue("expected-version")!))
    //                 .With<StreamRevision>("ActualRevision", long.Parse(rex.Trailers.GetValue("actual-version")!))
    //             ));
    //
    //         // var error = rex.AsStreamRevisionConflictError();
    //         //
    //         // return error.Metadata.GetRequired<long>("ActualRevision") == ExpectedStreamState.NoStream
    //         //     ? Result.Failure<LogPosition, DeleteStreamError>(new NotFound(x => x.WithStreamName(stream)))
    //         //     : Result.Failure<LogPosition, DeleteStreamError>(error);
    //     }
    //     catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted)) {
    //         return await GetStreamInfo(stream, cancellationToken).MatchAsync(
    //             info => info.State switch {
    //                 StreamState.Deleted    => Result.Failure<LogPosition, DeleteStreamError>(new StreamDeleted()),
    //                 StreamState.Tombstoned => Result.Failure<LogPosition, DeleteStreamError>(new StreamTombstoned())
    //             },
    //             err => err.ForwardErrors<DeleteStreamError>()
    //         );
    //     }
    //     catch (RpcException rex) {
    //         return Result.Failure<LogPosition, DeleteStreamError>(rex.StatusCode switch {
    //             StatusCode.PermissionDenied => new AccessDenied(),
    //             _                           => throw rex.WithOriginalCallStack()
    //         });
    //     }
    // }

    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
        var request = Requests.CreateDeleteRequest(stream, revision);

        try {
            var resp = await LegacyServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp.Position.MapToLogPosition();
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted) || rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
            return await GetStreamInfo(stream, cancellationToken).MatchAsync(
                info => info.State switch {
                    StreamState.Active     => Result.Failure<LogPosition, DeleteStreamError>(new StreamRevisionConflict()),
                    StreamState.Deleted    => Result.Failure<LogPosition, DeleteStreamError>(new StreamDeleted()),
                    StreamState.Tombstoned => Result.Failure<LogPosition, DeleteStreamError>(new StreamTombstoned()),
                    StreamState.Missing    => Result.Failure<LogPosition, DeleteStreamError>(new NotFound()),
                },
                err => err.ForwardErrors<DeleteStreamError>()
            );
        }
        catch (RpcException rex) {
            return Result.Failure<LogPosition, DeleteStreamError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }


    public ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, CancellationToken cancellationToken = default) =>
        Delete(stream, StreamRevision.Unset, cancellationToken);

    public async ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
        var request = Requests.CreateTombstoneRequest(stream, revision);

        try {
            var resp = await LegacyServiceClient
                .TombstoneAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted) || rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
            return await GetStreamInfo(stream, cancellationToken).MatchAsync(
                info => info.State switch {
                    StreamState.Active     => Result.Failure<LogPosition, TombstoneError>(new StreamRevisionConflict()),
                    StreamState.Deleted    => Result.Failure<LogPosition, TombstoneError>(new StreamDeleted()),
                    StreamState.Tombstoned => Result.Failure<LogPosition, TombstoneError>(new StreamTombstoned()),
                    StreamState.Missing    => Result.Failure<LogPosition, TombstoneError>(new NotFound()),
                },
                err => err.ForwardErrors<TombstoneError>()
            );
        }
        catch (RpcException rex) {
            return Result.Failure<LogPosition, TombstoneError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    // public async ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
    //     var request = Requests.CreateTombstoneRequest(stream, revision);
    //
    //     try {
    //         var resp = await LegacyServiceClient
    //             .TombstoneAsync(request, cancellationToken: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
    //     }
    //     catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
    //         // the request must be done with StreamExists expectation,
    //         // if the actual version is -1, it means the stream was not found
    //         // this was a tribal knowledge discovery in the original codebase
    //         // that for whatever reason I 100% think it is a bug in the server
    //
    //         var reVisionConflictError = rex.AsStreamRevisionConflictError();
    //         return reVisionConflictError.Metadata.GetRequired<StreamRevision>("ActualRevision") == ExpectedStreamState.NoStream.Value
    //             ? Result.Failure<LogPosition, TombstoneError>(new NotFound(x => x.WithStreamName(stream)))
    //             : Result.Failure<LogPosition, TombstoneError>(reVisionConflictError);
    //     }
    //     catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted)) {
    //         return await GetStreamInfo(stream, cancellationToken).MatchAsync(
    //             info => info.State switch {
    //                 StreamState.Deleted    => Result.Failure<LogPosition, TombstoneError>(new StreamDeleted(x => x.WithStreamName(stream))),
    //                 StreamState.Tombstoned => Result.Failure<LogPosition, TombstoneError>(new StreamTombstoned(x => x.WithStreamName(stream)))
    //             },
    //             err => Result.Failure<LogPosition, TombstoneError>(err.AsAccessDenied)
    //         );
    //     }
    //     catch (RpcException rex) {
    //         return Result.Failure<LogPosition, TombstoneError>(rex.StatusCode switch {
    //             StatusCode.PermissionDenied => new AccessDenied(),
    //             _                           => throw rex.WithOriginalCallStack()
    //         });
    //     }
    // }

    public ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, CancellationToken cancellationToken = default) =>
        Tombstone(stream, StreamRevision.Unset, cancellationToken);

    public ValueTask<Result<Success, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(truncateRevision, StreamRevision.Min);

        // var getStreamInfoResult = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
        //
        // if (getStreamInfoResult.IsFailure)
        //     return Result.Failure<Success, TruncateStreamError>(getStreamInfoResult.Error.AsAccessDenied);
        //
        // var info = getStreamInfoResult.Value;
        //
        // switch (info.State) {
        //     case StreamState.Deleted:    return Result.Failure<Success, TruncateStreamError>(new StreamDeleted());
        //     case StreamState.Tombstoned: return Result.Failure<Success, TruncateStreamError>(new StreamTombstoned());
        //     case StreamState.Missing:    return Result.Failure<Success, TruncateStreamError>(new NotFound());
        //
        //     default:
        //         var actualRevision = info.LastStreamRevision;
        //         var maxRevision    = actualRevision + 1; // all existing revisions
        //
        //         if (truncateRevision > maxRevision)
        //             return Result.Failure<Success, TruncateStreamError>(new StreamRevisionConflict(mt => mt.With("Reason", "Truncate revision cannot be greater than the last stream revision plus one.")));
        //
        //         var metadata = new StreamMetadata { TruncateBefore = truncateRevision }; // info.Metadata with { TruncateBefore = truncateRevision };
        //         return await SetStreamMetadata(stream, metadata, info.MetadataRevision, cancellationToken)
        //             .MatchAsync(
        //                 _   => Result.Success<Success, TruncateStreamError>(Success.Instance),
        //                 err => Result.Failure<Success, TruncateStreamError>((TruncateStreamError)err.Value)
        //             )
        //             .ConfigureAwait(false);
        // }

        return GetStreamInfo(stream, cancellationToken).MatchAsync(
            onSuccess: static async (info, state) => {
                switch (info.State) {
                    case StreamState.Deleted:    return Result.Failure<Success, TruncateStreamError>(new StreamDeleted());
                    case StreamState.Tombstoned: return Result.Failure<Success, TruncateStreamError>(new StreamTombstoned());
                    case StreamState.Missing:    return Result.Failure<Success, TruncateStreamError>(new NotFound());

                    default:
                        var actualRevision = info.LastStreamRevision;
                        var maxRevision    = actualRevision + 1; // all existing revisions

                        if (state.TruncateRevision > maxRevision)
                            return Result.Failure<Success, TruncateStreamError>(new StreamRevisionConflict(mt => mt.With("Reason", "Truncate revision cannot be greater than the last stream revision plus one.")));

                        var metadata = new StreamMetadata { TruncateBefore = state.TruncateRevision }; // info.Metadata with { TruncateBefore = truncateRevision };
                        return await state.Client.SetStreamMetadata(state.Stream, metadata, info.MetadataRevision, state.CancellationToken)
                            .MatchAsync(
                                _   => Result.Success<Success, TruncateStreamError>(Results.Success),
                                err => err.ForwardErrors<TruncateStreamError>()
                            )
                            .ConfigureAwait(false);
                }
            },
            onFailure: (err, _) => Result.FailureTask<Success, TruncateStreamError>(err.AsAccessDenied),
            state: (Stream: stream, TruncateRevision: truncateRevision, CancellationToken: cancellationToken, Client: this)
        );
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, StreamRevision expectedRevision, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);
        var message    = Message.Create(metadata);

        ExpectedStreamState expectedState = expectedRevision > StreamRevision.Min ? expectedRevision : ExpectedStreamState.Any;

        return this.Append(metaStream, expectedState, message, cancellationToken).MatchAsync(
            ok => Result.Success<StreamRevision, SetStreamMetadataError>(ok.StreamRevision),
            ko => ko.ForwardErrors<SetStreamMetadataError>()
        );
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, CancellationToken cancellationToken = default) =>
        SetStreamMetadata(stream, metadata, StreamRevision.Unset, cancellationToken);

    public record StreamDataRetentionOptions {
        public int?      MaxCount { get; init; }
        public TimeSpan? MaxAge   { get; init; }
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> ConfigureStreamDataRetention(StreamName stream, StreamDataRetentionOptions options, StreamRevision metaStreamRevision, CancellationToken cancellationToken) {
        var metadata = new StreamMetadata { MaxCount = options.MaxCount, MaxAge = options.MaxAge };
        return SetStreamMetadata(stream, metadata, metaStreamRevision, cancellationToken);
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> ConfigureStreamDataRetention(StreamName stream, StreamDataRetentionOptions options, CancellationToken cancellationToken) =>
        ConfigureStreamDataRetention(stream, options, StreamRevision.Unset, cancellationToken);

    // public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, int? maxCount, TimeSpan? maxAge, CancellationToken cancellationToken) =>
    //     SetDataRetention(stream, maxCount, maxAge, StreamRevision.Unset, cancellationToken);
    //
    // public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, int maxCount, CancellationToken cancellationToken) {
    //     ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
    //     ArgumentOutOfRangeException.ThrowIfGreaterThan(maxCount, int.MaxValue);
    //     return SetDataRetention(stream, maxCount, null, StreamRevision.Unset, cancellationToken);
    // }
    //
    // public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, TimeSpan maxAge, CancellationToken cancellationToken) {
    //     ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxAge, TimeSpan.Zero);
    //     ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(maxAge, TimeSpan.MaxValue);
    //     return SetDataRetention(stream, null, maxAge, StreamRevision.Unset, cancellationToken);
    // }


    // public async ValueTask<Result<Success, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) {
    //     ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(truncateRevision, StreamRevision.Min);
    //
    //     var getStreamInfoResult = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
    //
    //     if (getStreamInfoResult.IsFailure)
    //         return Result.Failure<Success, TruncateStreamError>(getStreamInfoResult.Error.AsAccessDenied);
    //
    //     var info = getStreamInfoResult.Value;
    //
    //     switch (info.State) {
    //         case StreamState.Deleted:    return Result.Failure<Success, TruncateStreamError>(new StreamDeleted());
    //         case StreamState.Tombstoned: return Result.Failure<Success, TruncateStreamError>(new StreamTombstoned());
    //         case StreamState.Missing:    return Result.Failure<Success, TruncateStreamError>(new NotFound());
    //
    //         case StreamState.Active:
    //             var actualRevision = info.LastStreamRevision;
    //             var maxRevision    = actualRevision + 1; // all existing revisions
    //
    //             if (truncateRevision > maxRevision)
    //                 return Result.Failure<Success, TruncateStreamError>(new StreamRevisionConflict(mt => mt.With("Reason", "Truncate revision cannot be greater than the last stream revision plus one.")));
    //
    //             var metadata = new StreamMetadata { TruncateBefore = truncateRevision }; // info.Metadata with { TruncateBefore = truncateRevision };
    //             return await SetStreamMetadata(stream, metadata, info.MetadataRevision, cancellationToken)
    //                 .MatchAsync(
    //                     _   => Result.Success<Success, TruncateStreamError>(Success.Instance),
    //                     err => Result.Failure<Success, TruncateStreamError>((TruncateStreamError)err.Value)
    //                 )
    //                 .ConfigureAwait(false);
    //     }
    //
    //     // var streamInfo = await GetStreamInfo(stream, cancellationToken)
    //     //     .MatchAsync(
    //     //         async info => {
    //     //             if (info.State == StreamState.Active) {
    //     //                 var actualRevision = info.LastStreamRevision;
    //     //                 var maxRevision    = actualRevision + 1; // all existing revisions
    //     //                 if (truncateRevision > maxRevision)
    //     //                     return Result.Failure<Success, TruncateStreamError>(new StreamRevisionConflict(mt => mt.With("Reason", "Truncate revision cannot be greater than the last stream revision plus one.")));
    //     //
    //     //                 var metadata = new StreamMetadata { TruncateBefore = truncateRevision };
    //     //                 return await SetStreamMetadata(stream, metadata, cancellationToken)
    //     //                     .MatchAsync(
    //     //                         rev => Result.Success<Success, TruncateStreamError>(Success.Instance),
    //     //                         err => (TruncateStreamError)err.Value
    //     //                     );
    //     //             }
    //     //             else {
    //     //                 return info.State switch {
    //     //                     StreamState.Deleted    => Result.Failure<Success, TruncateStreamError>(new StreamDeleted()),
    //     //                     StreamState.Tombstoned => Result.Failure<Success, TruncateStreamError>(new StreamTombstoned()),
    //     //                     StreamState.Missing    => Result.Failure<Success, TruncateStreamError>(new NotFound())
    //     //                 };
    //     //             }
    //     //
    //     //         },
    //     //         err => Result.FailureTask<Success, TruncateStreamError>(err.AsAccessDenied)
    //     //     );
    //
    //
    //     // var streamInfo = await GetStreamInfo(stream, cancellationToken)
    //     //     .MatchAsync(
    //     //         info => {
    //     //             if (info.State == StreamState.Active) {
    //     //                 var actualRevision = info.LastStreamRevision;
    //     //                 if (truncateRevision > actualRevision)
    //     //                     return Result.Failure<Success, TruncateStreamError>(new StreamRevisionConflict());
    //     //             }
    //     //
    //     //             info.State switch {
    //     //                 StreamState.Active     => Result.Success<Success, TruncateStreamError>(info),
    //     //                 StreamState.Deleted    => Result.Failure<Success, TruncateStreamError>(new StreamDeleted(x => x.WithStreamName(stream))),
    //     //                 StreamState.Tombstoned => Result.Failure<Success, TruncateStreamError>(new StreamTombstoned(x => x.WithStreamName(stream))),
    //     //                 _                      => Result.Failure<Success, TruncateStreamError>(new NotFound(x => x.WithStreamName(stream)))
    //     //             }
    //     //
    //     //         },
    //     //         err => Result.Failure<Success, TruncateStreamError>(err.AsAccessDenied)
    //     //     );
    //
    //
    //     // var metadata = new StreamMetadata { TruncateBefore = truncateRevision };
    //     // return SetStreamMetadata(stream, metadata, cancellationToken).MatchAsync(
    //     //     rev => Result.Success<bool, TruncateStreamError>(true),
    //     //     err => (TruncateStreamError)err.Value);
    // }
}
