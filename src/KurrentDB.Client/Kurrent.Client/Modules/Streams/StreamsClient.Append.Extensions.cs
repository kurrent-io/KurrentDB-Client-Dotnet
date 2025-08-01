using Kurrent.Client.Model;

namespace Kurrent.Client.Streams;

public static partial class StreamsClientExtensions {
    #region . Append .

    public static ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(this StreamsClient client, IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
        client.Append(requests.ToAsyncEnumerable(), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(this StreamsClient client, MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
        client.Append(request.Requests.ToAsyncEnumerable(), cancellationToken);

    /// <summary>
    /// Appends a series of messages to a specified stream in KurrentDB.
    /// </summary>
    /// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    public static async ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this StreamsClient client, AppendStreamRequest request, CancellationToken cancellationToken) {
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
    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this StreamsClient client, StreamName stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this StreamsClient client, StreamName stream, ExpectedStreamState expectedState, Message message, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

    public static ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(this StreamsClient client, StreamName stream, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        client.Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, messages), cancellationToken);

    #endregion
}
