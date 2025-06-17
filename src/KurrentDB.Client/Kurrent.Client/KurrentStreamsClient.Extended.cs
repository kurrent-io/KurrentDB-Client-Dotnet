using Kurrent.Client.Model;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    #region . Append .

    public ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(IEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default) =>
        Append(requests.ToAsyncEnumerable(), cancellationToken);

    public ValueTask<Result<AppendStreamSuccesses, AppendStreamFailures>> Append(MultiStreamAppendRequest request, CancellationToken cancellationToken = default) =>
        Append(request.Requests.ToAsyncEnumerable(), cancellationToken);

    /// <summary>
    /// Appends a series of messages to a specified stream in KurrentDB.
    /// </summary>
    /// <param name="request">The request object that specifies the stream, expected state, and messages to append.</param>
    /// <param name="cancellationToken">A token to cancel the operation if needed.</param>
    public async ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(AppendStreamRequest request, CancellationToken cancellationToken) {
        var result = await Append([request], cancellationToken).ConfigureAwait(false);

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
    public ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(string stream, ExpectedStreamState expectedState, IEnumerable<Message> messages, CancellationToken cancellationToken) =>
        Append(new AppendStreamRequest(stream, expectedState, messages), cancellationToken);

    public ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(string stream, ExpectedStreamState expectedState, Message message, CancellationToken cancellationToken) => Append(new AppendStreamRequest(stream, expectedState, [message]), cancellationToken);

    public ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append(string stream, List<Message> messages, CancellationToken cancellationToken) =>
        Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, messages), cancellationToken);

    // public ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append<T>(string stream, ExpectedStreamState expectedState, T message, CancellationToken cancellationToken) =>
    //     Append(new AppendStreamRequest(stream, expectedState, [new Message { Value = message ?? throw new ArgumentNullException(nameof(message)) }]), cancellationToken);
    //
    // public ValueTask<Result<AppendStreamSuccess, AppendStreamFailure>> Append<T>(string stream, T message, CancellationToken cancellationToken) =>
    //     Append(new AppendStreamRequest(stream, ExpectedStreamState.Any, [new Message { Value = message ?? throw new ArgumentNullException(nameof(message)) }]), cancellationToken);

    #endregion
}
