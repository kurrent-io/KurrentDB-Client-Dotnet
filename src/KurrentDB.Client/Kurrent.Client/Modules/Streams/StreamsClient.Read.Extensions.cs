namespace Kurrent.Client.Streams;

public static partial class StreamsClientExtensions {
    public static ValueTask<Result<Record, InspectRecordError>> InspectRecord(this StreamsClient client, LogPosition position, CancellationToken cancellationToken = default) =>
        client.InspectRecord(position, true, cancellationToken);

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

    // public static ValueTask<Result<Messages, ReadError>> ReadStreamBackwards(this StreamsClient client, StreamName stream, CancellationToken cancellationToken) =>
    //     client.ReadStream(options => options with { Stream = stream, Direction = ReadDirection.Backwards, CancellationToken = cancellationToken });

    public static ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this StreamsClient client, StreamName stream, bool skipDecoding, CancellationToken cancellationToken = default) =>
        client.ReadStreamEdge(stream, ReadDirection.Forwards, skipDecoding, cancellationToken);

    public static ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this StreamsClient client, StreamName stream, bool skipDecoding, CancellationToken cancellationToken = default) =>
        client.ReadStreamEdge(stream, ReadDirection.Backwards, skipDecoding, cancellationToken);

    public static ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this StreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadFirstStreamRecord(client, stream, true, cancellationToken);

    public static ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this StreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadLastStreamRecord(client, stream, true, cancellationToken);

    // static async ValueTask<Result<Record, ReadError>> ReadStreamEdge(this StreamsClient client, StreamName stream, ReadDirection direction, CancellationToken cancellationToken) {
    //     try {
    //         var options = new ReadStreamOptions {
    //             Stream            = stream,
    //             Direction         = direction,
    //             Start             = direction == ReadDirection.Forwards ? StreamRevision.Min : StreamRevision.Max,
    //             Limit             = 1,
    //             Heartbeat         = HeartbeatOptions.Disabled,
    //             CancellationToken = cancellationToken
    //         };
    //
    //         return await client.ReadStream(options)
    //             .MatchAsync(
    //                 async messages => {
    //                     var rec = await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken);
    //                     return rec ?? Result.Failure<Record, ReadError>(new ErrorDetails.NotFound(x => x.WithStreamName(stream)));
    //                 },
    //                 async err => await Result.FailureTask<Record, ReadError>(err)
    //             )
    //             .ConfigureAwait(false);
    //
    //         // var result = await client
    //         //     .ReadStream(options)
    //         //     .MatchAsync(
    //         //         async messages => {
    //         //             var rec = await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken);
    //         //             return rec ?? Result.Failure<Record, ReadError>(new ErrorDetails.StreamNotFound(x => x.WithStreamName(stream)));
    //         //         },
    //         //         async err => Result.Failure<Record, ReadError>(err))
    //         //     //.MapAsync(async messages => await messages.Select(x => x.AsRecord).FirstOrDefaultAsync(cancellationToken) ?? Record.None)
    //         //     .ConfigureAwait(false);
    //         //
    //         // return result.IsSuccess
    //         //     ? result.Value == Record.None
    //         //         ? Result.Failure<Record, ReadError>(new ErrorDetails.StreamNotFound())
    //         //         : result
    //         //     : result;
    //     }
    //     catch (Exception ex) when (ex is not KurrentException)  {
    //         throw KurrentException.CreateUnknown(
    //             direction == ReadDirection.Forwards ? nameof(ReadFirstStreamRecord) : nameof(ReadLastStreamRecord), ex);
    //     }
    // }
}
