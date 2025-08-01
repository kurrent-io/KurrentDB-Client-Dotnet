using Kurrent.Client.Model;

namespace Kurrent.Client.Streams;

public static partial class StreamsClientExtensions {

    public static ValueTask<Result<Messages, ReadError>> ReadAll(this StreamsClient client, Func<ReadAllOptions, ReadAllOptions>? configure = null) {
        var options = new ReadAllOptions();
        options = configure?.Invoke(options) ?? options;
        return client.ReadAll(options);
    }

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this StreamsClient client, Func<ReadStreamOptions, ReadStreamOptions>? configure = null) {
        var options = new ReadStreamOptions();
        options = configure?.Invoke(options) ?? options;
        return client.ReadStream(options);
    }

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this StreamsClient client, StreamName stream, StreamRevision start, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, Start = start, CancellationToken = cancellationToken });

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this StreamsClient client, StreamName stream, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, CancellationToken = cancellationToken });

    public static ValueTask<Result<Messages, ReadError>> ReadStreamBackwards(this StreamsClient client, StreamName stream, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, Direction = ReadDirection.Backwards, CancellationToken = cancellationToken });

    public static ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this StreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client,stream, ReadDirection.Forwards, cancellationToken);

    public static ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this StreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client,stream, ReadDirection.Backwards, cancellationToken);

    static async ValueTask<Result<Record, ReadError>> ReadStreamEdge(this StreamsClient client, StreamName stream, ReadDirection direction, CancellationToken cancellationToken) {
        try {
            var options = new ReadStreamOptions {
                Stream            = stream,
                Direction         = direction,
                Start             = direction == ReadDirection.Forwards ? StreamRevision.Min : StreamRevision.Max,
                Limit             = 1,
                Heartbeat         = HeartbeatOptions.Disabled,
                CancellationToken = cancellationToken
            };

            return await client.ReadStream(options)
                .MatchAsync(
                    async messages => {
                        var rec = await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken);
                        return rec ?? Result.Failure<Record, ReadError>(new ErrorDetails.StreamNotFound(x => x.With("stream", stream)));
                    },
                    async err => await Result.FailureTask<Record, ReadError>(err)
                )
                .ConfigureAwait(false);

            // var result = await client
            //     .ReadStream(options)
            //     .MatchAsync(
            //         async messages => {
            //             var rec = await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken);
            //             return rec ?? Result.Failure<Record, ReadError>(new ErrorDetails.StreamNotFound(x => x.With("stream", stream)));
            //         },
            //         async err => Result.Failure<Record, ReadError>(err))
            //     //.MapAsync(async messages => await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken) ?? Record.None)
            //     .ConfigureAwait(false);
            //
            // return result.IsSuccess
            //     ? result.Value == Record.None
            //         ? Result.Failure<Record, ReadError>(new ErrorDetails.StreamNotFound())
            //         : result
            //     : result;
        }
        catch (Exception ex) when (ex is not KurrentClientException)  {
            throw KurrentClientException.CreateUnknown(
                direction == ReadDirection.Forwards ? nameof(ReadFirstStreamRecord) : nameof(ReadLastStreamRecord), ex);
        }
    }
}
