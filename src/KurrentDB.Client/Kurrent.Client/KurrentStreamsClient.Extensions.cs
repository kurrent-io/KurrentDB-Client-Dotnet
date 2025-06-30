using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client;

public static class KurrentStreamsClientExtensions {
    #region . Append .

    public static ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(this KurrentStreamsClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
        client.Append(requests.ToAsyncEnumerable(), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(this KurrentStreamsClient client, MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
        client.Append(request.Requests.ToAsyncEnumerable(), cancellationToken);

    /// <summary>
    /// Appends a series of messages to a specified stream in KurrentDB.
    /// </summary>
    /// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    public static async ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, AppendStreamRequest request, CancellationToken cancellationToken) {
        var result = await client.Append([request], cancellationToken).ConfigureAwait(false);

        return result.Match<Result<AppendStreamSuccess, AppendStreamFailure>>(
            success => success.First(),
            failure => failure.First()
        );
    }

    /// <summary>
    /// Appends a series of messages to a specified stream while specifying the expected stream state.
    /// </summary>
    /// <param name="stream">The name of the stream to which the messages will be appended.</param>
    /// <param name="expectedState">The expected state of the stream to ensure consistency during the append operation.</param>
    /// <param name="messages">A collection of messages to be appended to the stream.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete, allowing for cancellation if needed.</param>
    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, string stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, string stream, ExpectedStreamState expectedState, Message message, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, string stream, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, messages), cancellationToken);

    #endregion

    #region . Read .

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this KurrentStreamsClient client, StreamName stream, Func<ReadStreamOptions, ReadStreamOptions>? configure = null) {
        var options = new ReadStreamOptions(
            StreamRevision.Min,
            long.MaxValue,
            ReadDirection.Forwards,
            CancellationToken.None
        );

        options = configure?.Invoke(options) ?? options;

        return client.ReadStream(stream, options);
    }

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this KurrentStreamsClient client, StreamName stream, StreamRevision start, CancellationToken cancellationToken) =>
        client.ReadStream(stream, options => options with { StartRevision = start, CancellationToken = cancellationToken });

    static async ValueTask<Result<Record, ReadError>> ReadStreamEdge(this KurrentStreamsClient client, StreamName stream, ReadStreamOptions options) {
        try {
            var result = await client
                .ReadStream(stream, options)
                .MapAsync(
                    static (msgs, ct) => msgs
                        .Select(msg => msg.Match(record => record, heartbeat => Record.None))
                        .FirstOrDefaultAsync(ct),
                    state: options.CancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) when (ex is not KurrentClientException)  {
            throw KurrentClientException.CreateUnknown(
                options.Direction == ReadDirection.Forwards ? nameof(ReadFirstStreamRecord) : nameof(ReadLastStreamRecord), ex);
        }
    }

    public static ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client, stream, ReadStreamOptions.FirstRecord(cancellationToken));

    public static ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client, stream, ReadStreamOptions.LastRecord(cancellationToken));

    // public static async ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) {
    //     try {
    //         var result = await client
    //             .ReadStream(stream, ReadStreamOptions.FirstRecord(cancellationToken))
    //             .MapAsync(
    //                 static (msgs, ct) => msgs
    //                     .Select(msg => msg.Match(record => record, heartbeat => Record.None))
    //                     .FirstOrDefaultAsync(ct),
    //                 state: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         return result;
    //     }
    //     catch (Exception ex) when (ex is not KurrentClientException)  {
    //         throw KurrentClientException.CreateUnknown(nameof(ReadFirstStreamRecord), ex);
    //     }
    // }
    //
    // public static async ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) {
    //     try {
    //         var result = await client
    //             .ReadStream(stream, ReadStreamOptions.LastRecord(cancellationToken))
    //             .MapAsync(
    //                 static (msgs, ct) => msgs
    //                     .Select(msg => msg.Match(record => record, heartbeat => Record.None))
    //                     .FirstOrDefaultAsync(ct),
    //                 state: cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         return result;
    //     }
    //     catch (Exception ex) when (ex is not KurrentClientException)  {
    //         throw KurrentClientException.CreateUnknown(nameof(ReadFirstStreamRecord), ex);
    //     }
    // }

    // public static async IAsyncEnumerable<ReadMessage> ReadStream(
    //     this KurrentStreamsClient client
    //     string stream, LogPosition startPosition, long limit, Direction direction,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var revision = startPosition switch {
    //         _ when startPosition == LogPosition.Unset    => StreamRevision.Min,
    //         _ when startPosition == LogPosition.Earliest => StreamRevision.Min,
    //         _ when startPosition == LogPosition.Latest   => StreamRevision.Max,
    //         _                                            => await client.GetStreamRevision(startPosition, cancellationToken).ConfigureAwait(false)
    //     };
    //
    //     var session = ReadStream(
    //         stream, revision, limit,
    //         direction, cancellationToken
    //     );
    //
    //     await foreach (var record in session.ConfigureAwait(false))
    //         yield return record;
    // }

    #endregion
}
