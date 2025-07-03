#pragma warning disable CS8509

using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;
using static Kurrent.Client.Model.ErrorDetails;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
        var request = StreamsClientV1Mapper.Requests.CreateDeleteRequest(stream, revision);

        try {
            var resp = await ServiceClientV1
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            // if the actual version is -1, it means the stream was not found
            // this was a tribal knowledge discovery in the original codebase
            // that for whatever reason I 100% think it is a bug in the server
            return Result.Failure<LogPosition, DeleteStreamError>(
                ex.ActualVersion == -1
                    ? new StreamNotFound(x => x.With("stream", stream))
                    : ex.AsStreamRevisionConflict());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, DeleteStreamError>(ex switch {
                StreamNotFoundException       => ex.AsStreamNotFoundError(),
                AccessDeniedException         => ex.AsAccessDeniedError(stream),
                _                             => throw KurrentClientException.CreateUnknown(nameof(Delete), ex)
            });
        }
    }

    public ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, CancellationToken cancellationToken = default) =>
        Delete(stream, StreamRevision.Unset, cancellationToken);

    public async ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, StreamRevision revision, CancellationToken cancellationToken = default) {
        var request = StreamsClientV1Mapper.Requests.CreateTombstoneRequest(stream, revision);

        try {
            var resp = await ServiceClientV1
                .TombstoneAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            // if the actual version is -1, it means the stream was not found
            // this was a tribal knowledge discovery in the original codebase
            // that for whatever reason I 100% think it is a bug in the server
            return Result.Failure<LogPosition, TombstoneError>(
                ex.ActualVersion == -1
                    ? new StreamNotFound(x => x.With("stream", stream))
                    : ex.AsStreamRevisionConflict());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, TombstoneError>(ex switch {
                StreamNotFoundException       => ex.AsStreamNotFoundError(),
                AccessDeniedException         => ex.AsAccessDeniedError(stream),
                _                             => throw KurrentClientException.CreateUnknown(nameof(Tombstone), ex)
            });
        }
    }

    public ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, CancellationToken cancellationToken = default) =>
        Tombstone(stream, StreamRevision.Unset, cancellationToken);

    public ValueTask<Result<bool, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, ExpectedStreamState expectedState, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(truncateRevision, StreamRevision.Unset);

        var metaddata = new StreamMetadata { TruncateBefore = truncateRevision };
        return SetStreamMetadata(stream, metaddata, expectedState, cancellationToken).MatchAsync(
            rev => Result.Success<bool, TruncateStreamError>(true),
            err => (TruncateStreamError)err.Value);
    }

    public ValueTask<Result<bool, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, CancellationToken cancellationToken) =>
        Truncate(stream, truncateRevision, ExpectedStreamState.Any, cancellationToken);

    public ValueTask<Result<bool, StreamExistsError>> StreamExists(StreamName stream, CancellationToken cancellationToken = default) =>
        this.ReadFirstStreamRecord(stream, cancellationToken).MatchAsync(
            rec => Result.Success<bool, StreamExistsError>(true),
            err => err.Value switch {
                AccessDenied denied => Result.Failure<bool, StreamExistsError>(denied),
                _                   => Result.Success<bool, StreamExistsError>(false)
            });

    public async ValueTask<Result<StreamInfo, GetStreamInfoError>> GetStreamInfo(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        // read the stream metadata from the last record of the metastream
        var metastreamReadResult = await this.ReadLastStreamRecord(metaStream, cancellationToken)
            .MatchAsync(
                rec => new StreamInfo {
                    Metadata         = rec == Record.None ? StreamMetadata.None : (StreamMetadata)rec.Value!,
                    MetadataRevision = rec.StreamRevision
                },
                err => err.IsAccessDenied
                    ? Result.Failure<StreamInfo, GetStreamInfoError>(err.AsAccessDenied)
                    : new StreamInfo()
            )
            .ConfigureAwait(false);

        // access denied
        if (metastreamReadResult.IsFailure)
            return metastreamReadResult;

        // check if the stream is actually deleted and if not,
        // sets the latest position and revision
        return await this.ReadLastStreamRecord(stream, cancellationToken)
            .MatchAsync(
                rec => metastreamReadResult.Value with {
                    LastStreamPosition = rec.Position,
                    LastStreamRevision = rec.StreamRevision
                },
                err => err.IsStreamDeleted
                    ? metastreamReadResult.Value with { IsDeleted = true }
                    : Result.Failure<StreamInfo, GetStreamInfoError>((GetStreamInfoError)err.Value)
            )
            .ConfigureAwait(false);
    }

    public ValueTask<Result<StreamMetadata, GetStreamInfoError>> GetStreamMetadata(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);
        return this.ReadLastStreamRecord(metaStream, cancellationToken).MatchAsync(
            rec => rec == Record.None ? StreamMetadata.None : (StreamMetadata)rec.Value!,
            err => Result.Failure<StreamMetadata, GetStreamInfoError>((GetStreamInfoError)err.Value));
    }

	public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);
        return this.Append(metaStream, expectedState, Message.Create(metadata), cancellationToken).MatchAsync(
            ok => Result.Success<StreamRevision, SetStreamMetadataError>(ok.StreamRevision),
            ko => (SetStreamMetadataError)ko.Value);
    }
}
