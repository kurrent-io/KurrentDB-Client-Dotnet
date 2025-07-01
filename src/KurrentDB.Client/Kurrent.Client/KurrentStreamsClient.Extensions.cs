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
    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, StreamName stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, StreamName stream, ExpectedStreamState expectedState, Message message, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this KurrentStreamsClient client, StreamName stream, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, messages), cancellationToken);

    #endregion

    #region . Read .

    public static ValueTask<Result<Messages, ReadError>> ReadAll(this KurrentStreamsClient client, Func<ReadAllOptions, ReadAllOptions>? configure = null) {
        var options = new ReadAllOptions();
        options = configure?.Invoke(options) ?? options;
        return client.ReadAll(options);
    }

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this KurrentStreamsClient client, Func<ReadStreamOptions, ReadStreamOptions>? configure = null) {
        var options = new ReadStreamOptions();
        options = configure?.Invoke(options) ?? options;
        return client.ReadStream(options);
    }

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this KurrentStreamsClient client, StreamName stream, StreamRevision start, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, Start = start, CancellationToken = cancellationToken });

    public static ValueTask<Result<Messages, ReadError>> ReadStream(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, CancellationToken = cancellationToken });

    public static ValueTask<Result<Messages, ReadError>> ReadStreamBackwards(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken) =>
        client.ReadStream(options => options with { Stream = stream, Direction = ReadDirection.Backwards, CancellationToken = cancellationToken });

    public static ValueTask<Result<Record, ReadError>> ReadFirstStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client,stream, ReadDirection.Forwards, cancellationToken);

    public static ValueTask<Result<Record, ReadError>> ReadLastStreamRecord(this KurrentStreamsClient client, StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStreamEdge(client,stream, ReadDirection.Backwards, cancellationToken);

    static async ValueTask<Result<Record, ReadError>> ReadStreamEdge(this KurrentStreamsClient client, StreamName stream, ReadDirection direction, CancellationToken cancellationToken) {
        try {
            var result = await client
                .ReadStream(new() {
                    Stream            = stream,
                    Direction         = direction,
                    Start             = direction == ReadDirection.Forwards ? StreamRevision.Min : StreamRevision.Max,
                    Limit             = 1,
                    CancellationToken = cancellationToken
                })
                .MapAsync(
                    static (msgs, ct) => msgs.Select(msg => msg.Match(record => record, heartbeat => Record.None)).FirstOrDefaultAsync(ct),
                    state: cancellationToken)
                .ConfigureAwait(false);

            return result;
        }
        catch (Exception ex) when (ex is not KurrentClientException)  {
            throw KurrentClientException.CreateUnknown(
                direction == ReadDirection.Forwards ? nameof(ReadFirstStreamRecord) : nameof(ReadLastStreamRecord), ex);
        }
    }

    #endregion

    public static ValueTask<Result<Subscription, ReadError>> Subscribe(this KurrentStreamsClient client, Func<AllSubscriptionOptions, AllSubscriptionOptions>? configure = null) {
        var options = new AllSubscriptionOptions();
        options = configure?.Invoke(options) ?? options;
        return client.Subscribe(options);
    }

    public static ValueTask<Result<Subscription, ReadError>> Subscribe(this KurrentStreamsClient client, Func<StreamSubscriptionOptions, StreamSubscriptionOptions>? configure = null) {
        var options = new StreamSubscriptionOptions();
        options = configure?.Invoke(options) ?? options;
        return client.Subscribe(options);
    }
}
