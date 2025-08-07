#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using static Kurrent.Client.ErrorDetails;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
        var request = StreamsClientV1Mapper.Requests.CreateDeleteRequest(stream, revision);

        try {
            var resp = await LegacyServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
            // the request must be done with StreamExists expectation,
            // if the actual version is -1, it means the stream was not found
            // this was a tribal knowledge discovery in the original codebase
            // that for whatever reason I 100% think it is a bug in the server

            var reVisionConflictError = rex.AsStreamRevisionConflictError();
            return reVisionConflictError.Metadata.GetRequired<StreamRevision>("ActualRevision") == ExpectedStreamState.NoStream.Value
                ? Result.Failure<LogPosition, DeleteStreamError>(new StreamNotFound(x => x.With("Stream", stream)))
                : Result.Failure<LogPosition, DeleteStreamError>(reVisionConflictError);
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted)) {
            return await GetStreamInfo(stream, cancellationToken).MatchAsync(
                info => info.State switch {
                    StreamState.Deleted    => Result.Failure<LogPosition, DeleteStreamError>(new StreamDeleted(x => x.With("Stream", stream))),
                    StreamState.Tombstoned => Result.Failure<LogPosition, DeleteStreamError>(new StreamTombstoned(x => x.With("Stream", stream)))
                },
                err => Result.Failure<LogPosition, DeleteStreamError>(err.AsAccessDenied)
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
        var request = StreamsClientV1Mapper.Requests.CreateTombstoneRequest(stream, revision);

        try {
            var resp = await LegacyServiceClient
                .TombstoneAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.WrongExpectedVersion)) {
            // the request must be done with StreamExists expectation,
            // if the actual version is -1, it means the stream was not found
            // this was a tribal knowledge discovery in the original codebase
            // that for whatever reason I 100% think it is a bug in the server

            var reVisionConflictError = rex.AsStreamRevisionConflictError();
            return reVisionConflictError.Metadata.GetRequired<StreamRevision>("ActualRevision") == ExpectedStreamState.NoStream.Value
                ? Result.Failure<LogPosition, TombstoneError>(new StreamNotFound(x => x.With("Stream", stream)))
                : Result.Failure<LogPosition, TombstoneError>(reVisionConflictError);
        }
        catch (RpcException rex) when (rex.IsLegacyError(LegacyErrorCodes.StreamDeleted)) {
            return await GetStreamInfo(stream, cancellationToken).MatchAsync(
                info => info.State switch {
                    StreamState.Deleted    => Result.Failure<LogPosition, TombstoneError>(new StreamDeleted(x => x.With("Stream", stream))),
                    StreamState.Tombstoned => Result.Failure<LogPosition, TombstoneError>(new StreamTombstoned(x => x.With("Stream", stream)))
                },
                err => Result.Failure<LogPosition, TombstoneError>(err.AsAccessDenied)
            );
        }
        catch (RpcException rex) {
            return Result.Failure<LogPosition, TombstoneError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new AccessDenied(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
    }

    public ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, CancellationToken cancellationToken = default) =>
        Tombstone(stream, StreamRevision.Unset, cancellationToken);

    public ValueTask<Result<bool, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(truncateRevision, StreamRevision.Min);

        var metadata = new StreamMetadata { TruncateBefore = truncateRevision };
        return SetStreamMetadata(stream, metadata, cancellationToken).MatchAsync(
            rev => Result.Success<bool, TruncateStreamError>(true),
            err => (TruncateStreamError)err.Value);
    }

    public async ValueTask<Result<StreamInfo, GetStreamInfoError>> GetStreamInfo(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        var result = await this.ReadLastStreamRecord(metaStream, cancellationToken)
            .MatchAsync(
                rec => {
                    if (rec == Record.None) return new StreamInfo(); // no metadata stream, return empty info

                    var metadata  = (StreamMetadata)rec.Value!;
                    var isDeleted = metadata.TruncateBefore == StreamRevision.Max;
                    return new StreamInfo {
                        Metadata         = metadata,
                        MetadataRevision = rec.StreamRevision,
                        IsDeleted        = isDeleted,
                        State            = isDeleted ? StreamState.Deleted : StreamState.Active
                    };
                },
                err => err.Case switch {
                    ReadError.ReadErrorCase.StreamDeleted    => new StreamInfo { IsDeleted = true, IsTombstoned = true, State = StreamState.Tombstoned },
                    ReadError.ReadErrorCase.StreamTombstoned => new StreamInfo { IsDeleted = true, IsTombstoned = true, State = StreamState.Tombstoned },
                    ReadError.ReadErrorCase.StreamNotFound   => new StreamInfo(),
                    ReadError.ReadErrorCase.AccessDenied     => Result.Failure<StreamInfo, GetStreamInfoError>(err.AsAccessDenied)
                }
            )
            .ConfigureAwait(false);

        // if we failed to access the metadata stream, we return the error
        // if the metadata stream is deleted, we return the stream info with IsDeleted set to true
        if (result.IsFailure || result.Value.IsDeleted)
            return result;

        // now we enrich the stream info with the latest position and revision
        // because the metadata stream is not guaranteed to even exist regardless of the stream
        return await this.ReadLastStreamRecord(stream, cancellationToken)
            .MatchAsync(
                rec => result.Value with {
                    LastStreamPosition = rec.Position,
                    LastStreamRevision = rec.StreamRevision,
                    LastStreamUpdate   = rec.Timestamp
                },
                err => err.Case switch {
                    ReadError.ReadErrorCase.StreamDeleted    => result.Value with { IsDeleted = true, State = StreamState.Deleted },
                    ReadError.ReadErrorCase.StreamTombstoned => result,
                    ReadError.ReadErrorCase.StreamNotFound   => result,
                    ReadError.ReadErrorCase.AccessDenied     => Result.Failure<StreamInfo, GetStreamInfoError>(err.AsAccessDenied)
                }
            )
            .ConfigureAwait(false);
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, StreamRevision metaStreamRevision, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);
        var message    = Message.Create(metadata);

        ExpectedStreamState expectedState = metaStreamRevision > StreamRevision.Min
            ? metaStreamRevision : ExpectedStreamState.Any;

        return this.Append(metaStream, expectedState, message, cancellationToken).MatchAsync(
            ok => Result.Success<StreamRevision, SetStreamMetadataError>(ok.StreamRevision),
            // ko => Result.Failure<StreamRevision, SetStreamMetadataError>(ko.Case switch {
            //     AppendStreamFailure.AppendStreamFailureCase.StreamDeleted          => ko.AsStreamDeleted,
            //     AppendStreamFailure.AppendStreamFailureCase.StreamNotFound         => ko.AsStreamNotFound,
            //     AppendStreamFailure.AppendStreamFailureCase.AccessDenied           => ko.AsAccessDenied,
            //     AppendStreamFailure.AppendStreamFailureCase.StreamRevisionConflict => ko.AsStreamRevisionConflict
            // })
            // ko => Result.Failure<StreamRevision, SetStreamMetadataError>(ko.Value switch {
            //     StreamDeleted err          => err,
            //     StreamNotFound err         => err,
            //     AccessDenied err           => err,
            //     StreamRevisionConflict err => err,
            // })
            ko => Result.Failure<StreamRevision, SetStreamMetadataError>((SetStreamMetadataError)(dynamic)ko.Value)
        );
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, CancellationToken cancellationToken = default) =>
        SetStreamMetadata(stream, metadata, StreamRevision.Unset, cancellationToken);

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, int? maxCount, TimeSpan? maxAge, StreamRevision metaStreamRevision, CancellationToken cancellationToken) {
        var metadata = new StreamMetadata { MaxCount = maxCount, MaxAge = maxAge };
        return SetStreamMetadata(stream, metadata, metaStreamRevision, cancellationToken);
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, int? maxCount, TimeSpan? maxAge, CancellationToken cancellationToken) =>
        SetDataRetention(stream, maxCount, maxAge, StreamRevision.Unset, cancellationToken);

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, int maxCount, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxCount, int.MaxValue);
        return SetDataRetention(stream, maxCount, null, StreamRevision.Unset, cancellationToken);
    }

    public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetDataRetention(StreamName stream, TimeSpan maxAge, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxAge, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(maxAge, TimeSpan.MaxValue);
        return SetDataRetention(stream, null, maxAge, StreamRevision.Unset, cancellationToken);
    }

    public enum StreamState { Active, Deleted, Tombstoned, NotFound }
}
