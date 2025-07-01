#pragma warning disable CS8509

using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using KurrentDB.Client;
using static Kurrent.Client.Model.ErrorDetails;
using ErrorDetails = Kurrent.Client.Model.ErrorDetails;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    public async ValueTask<Result<LogPosition, DeleteStreamError>> Delete(StreamName stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var request = StreamsV1Mapper.CreateDeleteRequest(stream, expectedState);

        try {
            var resp = await LegacyStreamsClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            var info = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
            return Result.Failure<LogPosition, DeleteStreamError>(
                info is { IsFailure: true, Error.IsStreamNotFound: true }
                    ? info.Error.AsStreamNotFound
                    : ex.AsStreamRevisionConflict());
        }
        catch (Exception ex) {
            return Result.Failure<LogPosition, DeleteStreamError>(ex switch {
                StreamNotFoundException => ex.AsStreamNotFoundError(),
                AccessDeniedException   => ex.AsAccessDeniedError(stream),
                _                       => throw KurrentClientException.CreateUnknown(nameof(Delete), ex)
            });
        }
    }

    public async ValueTask<Result<LogPosition, TombstoneError>> Tombstone(StreamName stream, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var request = StreamsV1Mapper.CreateTombstoneRequest(stream, expectedState);

        try {
            var resp = await LegacyStreamsClient
                .TombstoneAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return resp?.Position?.MapToLogPosition() ?? LogPosition.Unset;
        }
        catch (WrongExpectedVersionException ex) {
            var info = await GetStreamInfo(stream, cancellationToken).ConfigureAwait(false);
            return Result.Failure<LogPosition, TombstoneError>(
                info is { IsFailure: true, Error.IsStreamNotFound: true }
                    ? info.Error.AsStreamNotFound
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

    public ValueTask<Result<bool, TruncateStreamError>> Truncate(StreamName stream, StreamRevision truncateRevision, ExpectedStreamState expectedState, CancellationToken cancellationToken) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(truncateRevision, StreamRevision.Unset);

        var metaddata = new StreamMetadata { TruncateBefore = truncateRevision };
        return SetStreamMetadata(stream, metaddata, expectedState, cancellationToken)
            .MapAsync(_ => true)
            .MapErrorAsync(err => err.Match<TruncateStreamError>(
                    notFound => notFound,
                    deleted => deleted,
                    denied => denied,
                    conflict => conflict
                )
            );
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
                    Metadata         = (StreamMetadata)rec.Value,
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
                    : Result.Failure<StreamInfo, GetStreamInfoError>(err.Value switch {
                        StreamNotFound x => x,
                        AccessDenied x   => x
                    })
            )
            .ConfigureAwait(false);
    }

    public async ValueTask<Result<StreamMetadata, GetStreamInfoError>> GetStreamMetadata(StreamName stream, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        var result = await this.ReadLastStreamRecord(metaStream, cancellationToken)
            .MatchAsync(
                rec => rec == Record.None ? StreamMetadata.None : (StreamMetadata)rec.Value,
                err => Result.Failure<StreamMetadata, GetStreamInfoError>(err.Value switch {
                    StreamNotFound notFound          => notFound,
                    ErrorDetails.AccessDenied denied => denied
                })
            )
            .ConfigureAwait(false);

        return result;
    }

	public ValueTask<Result<StreamRevision, SetStreamMetadataError>> SetStreamMetadata(StreamName stream, StreamMetadata metadata, ExpectedStreamState expectedState, CancellationToken cancellationToken = default) {
        var metaStream = SystemStreams.MetastreamOf(stream);

        return this.Append(metaStream, expectedState, Message.New.WithValue(metadata).Build(), cancellationToken)
            .MatchAsync(
                ok => ok.StreamRevision,
                ko => Result.Failure<StreamRevision, SetStreamMetadataError>(ko.Value switch {
                    StreamNotFound err         => err,
                    StreamDeleted err          => err,
                    AccessDenied err           => err,
                    StreamRevisionConflict err => err
                }));
    }
}
