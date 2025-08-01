using System.Threading.Channels;
using EventStore.Client;
using Grpc.Core;
using Kurrent.Client;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Protocol.PersistentSubscriptions.V1;
using static KurrentDB.Protocol.PersistentSubscriptions.V1.ReadResp.ContentOneofCase;
using AsyncStreamReaderExtensions = Kurrent.Client.AsyncStreamReaderExtensions;

namespace KurrentDB.Client;

partial class KurrentDBPersistentSubscriptionsClient {
    /// <summary>
    /// Subscribes to a persistent subscription.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    [Obsolete("SubscribeAsync is no longer supported. Use SubscribeToStream with manual acks instead.", false)]
    internal async Task<PersistentSubscription> SubscribeAsync(
        string streamName, string groupName,
        Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        UserCredentials? userCredentials = null, int bufferSize = 10, bool autoAck = true,
        CancellationToken cancellationToken = default
    ) {
        if (autoAck)
            throw new InvalidOperationException($"AutoAck is no longer supported. Please use {nameof(SubscribeToStream)} with manual acks instead.");

        return await PersistentSubscription
            .Confirm(
                SubscribeToStream(
                    streamName, groupName, bufferSize,
                    userCredentials, cancellationToken
                ),
                eventAppeared,
                subscriptionDropped ?? delegate { },
                _log,
                userCredentials,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Subscribes to a persistent subscription. Messages must be manually acknowledged
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    internal async Task<PersistentSubscription> SubscribeToStreamAsync(
        string streamName, string groupName,
        Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        UserCredentials? userCredentials = null, int bufferSize = 10,
        CancellationToken cancellationToken = default
    ) {
        return await PersistentSubscription
            .Confirm(
                SubscribeToStream(
                    streamName, groupName, bufferSize,
                    userCredentials, cancellationToken
                ),
                eventAppeared,
                subscriptionDropped ?? delegate { },
                _log,
                userCredentials,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Subscribes to a persistent subscription. Messages must be manually acknowledged.
    /// </summary>
    /// <param name="streamName">The name of the stream to read events from.</param>
    /// <param name="groupName">The name of the persistent subscription group.</param>
    /// <param name="bufferSize">The size of the buffer.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
    /// <returns></returns>
    internal PersistentSubscriptionResult SubscribeToStream(
        string streamName, string groupName, int bufferSize = 10,
        UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
    ) {
        if (streamName == null) throw new ArgumentNullException(nameof(streamName));

        if (groupName == null) throw new ArgumentNullException(nameof(groupName));

        if (streamName == string.Empty) throw new ArgumentException($"{nameof(streamName)} may not be empty.", nameof(streamName));

        if (groupName == string.Empty) throw new ArgumentException($"{nameof(groupName)} may not be empty.", nameof(groupName));

        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var readOptions = new ReadReq.Types.Options {
            BufferSize = bufferSize,
            GroupName  = groupName,
            UuidOption = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() }
        };

        if (streamName == SystemStreams.AllStream)
            readOptions.All = new Empty();
        else
            readOptions.StreamIdentifier = streamName;

        return new PersistentSubscriptionResult(
            streamName,
            groupName,
            async ct => {
                var channelInfo = await GetChannelInfo(ct).ConfigureAwait(false);

                if (streamName == SystemStreams.AllStream &&
                    !channelInfo.ServerCapabilities.SupportsPersistentSubscriptionsToAll)
                    throw new NotSupportedException("The server does not support persistent subscriptions to $all.");

                return channelInfo;
            },
            new() { Options = readOptions },
            Settings,
            userCredentials,
            cancellationToken
        );
    }

    /// <summary>
    /// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged
    /// </summary>
    internal async Task<PersistentSubscription> SubscribeToAllAsync(
        string groupName,
        Func<PersistentSubscription, ResolvedEvent, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        UserCredentials? userCredentials = null, int bufferSize = 10,
        CancellationToken cancellationToken = default
    ) =>
        await SubscribeToStreamAsync(
                SystemStreams.AllStream,
                groupName,
                eventAppeared,
                subscriptionDropped,
                userCredentials,
                bufferSize,
                cancellationToken
            )
            .ConfigureAwait(false);

    /// <summary>
    /// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged.
    /// </summary>
    /// <param name="groupName">The name of the persistent subscription group.</param>
    /// <param name="bufferSize">The size of the buffer.</param>
    /// <param name="userCredentials">The optional user credentials to perform operation with.</param>
    /// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
    /// <returns></returns>
    internal PersistentSubscriptionResult SubscribeToAll(
        string groupName, int bufferSize = 10,
        UserCredentials? userCredentials = null, CancellationToken cancellationToken = default
    ) =>
        SubscribeToStream(
            SystemStreams.AllStream, groupName, bufferSize,
            userCredentials, cancellationToken
        );

    /// <inheritdoc />
    internal class PersistentSubscriptionResult : IAsyncEnumerable<ResolvedEvent>, IAsyncDisposable, IDisposable {
        const    int                                    MaxEventIdLength = 2000;
        readonly CallOptions                            _callOptions;
        readonly Channel<PersistentSubscriptionMessage> _channel;
        readonly CancellationTokenSource                _cts;

        readonly ReadReq _request;

        AsyncDuplexStreamingCall<ReadReq, ReadResp>? _call;
        int                                          _messagesEnumerated;

        internal PersistentSubscriptionResult(
            string streamName, string groupName,
            Func<CancellationToken, Task<ChannelInfo>> selectChannelInfo,
            ReadReq request, KurrentDBClientSettings settings, UserCredentials? userCredentials,
            CancellationToken cancellationToken
        ) {
            StreamName = streamName;
            GroupName  = groupName;

            _request = request;

            _callOptions = KurrentDBCallOptions.CreateStreaming(
                settings,
                userCredentials: userCredentials,
                cancellationToken: cancellationToken
            );

            _channel = Channel.CreateBounded<PersistentSubscriptionMessage>(ReadBoundedChannelOptions);

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = PumpMessages();

            return;

            async Task PumpMessages() {
                try {
                    var channelInfo = await selectChannelInfo(_cts.Token).ConfigureAwait(false);
                    var client      = new PersistentSubscriptionsService.PersistentSubscriptionsServiceClient(channelInfo.CallInvoker);

                    _call = client.Read(_callOptions);

                    await _call.RequestStream.WriteAsync(_request).ConfigureAwait(false);

                    await foreach (var response in AsyncStreamReaderExtensions.ReadAllAsync(_call.ResponseStream, _cts.Token).ConfigureAwait(false)) {
                        PersistentSubscriptionMessage subscriptionMessage = response.ContentCase switch {
                            SubscriptionConfirmation => new PersistentSubscriptionMessage.SubscriptionConfirmation(
                                response.SubscriptionConfirmation.SubscriptionId
                            ),
                            Event => new PersistentSubscriptionMessage.Event(
                                ConvertToResolvedEvent(response),
                                response.Event.CountCase switch {
                                    ReadResp.Types.ReadEvent.CountOneofCase.RetryCount => response.Event.RetryCount,
                                    _                                                  => null
                                }
                            ),
                            _ => PersistentSubscriptionMessage.Unknown.Instance
                        };

                        if (subscriptionMessage is PersistentSubscriptionMessage.Event evnt)
                            KurrentDBClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
                                SubscriptionId,
                                evnt.ResolvedEvent,
                                channelInfo,
                                settings,
                                userCredentials
                            );

                        await _channel.Writer.WriteAsync(subscriptionMessage, _cts.Token).ConfigureAwait(false);
                    }

                    _channel.Writer.TryComplete();
                }
                catch (Exception ex) {
                    if (ex is PersistentSubscriptionNotFoundException) {
                        await _channel.Writer
                            .WriteAsync(PersistentSubscriptionMessage.NotFound.Instance, cancellationToken)
                            .ConfigureAwait(false);

                        _channel.Writer.TryComplete();
                        return;
                    }

                    _channel.Writer.TryComplete(ex);
                }
            }
        }

        /// <summary>
        /// The server-generated unique identifier for the subscription.
        /// </summary>
        public string? SubscriptionId { get; private set; }

        /// <summary>
        /// The name of the stream to read events from.
        /// </summary>
        public string StreamName { get; }

        /// <summary>
        /// The name of the persistent subscription group.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// An <see cref="IAsyncEnumerable{PersistentSubscriptionMessage}"/>. Do not enumerate more than once.
        /// </summary>
        public IAsyncEnumerable<PersistentSubscriptionMessage> Messages {
            get {
                if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1)
                    throw new InvalidOperationException("Messages may only be enumerated once.");

                return GetMessages();

                async IAsyncEnumerable<PersistentSubscriptionMessage> GetMessages() {
                    try {
                        await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)) {
                            if (message is PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId))
                                SubscriptionId = subscriptionId;

                            yield return message;
                        }
                    }
                    finally {
                        _cts.Cancel();
                    }
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            await CastAndDispose(_cts).ConfigureAwait(false);
            await CastAndDispose(_call).ConfigureAwait(false);

            return;

            static async Task CastAndDispose(IDisposable? resource) {
                switch (resource) {
                    case null:
                        return;

                    case IAsyncDisposable resourceAsyncDisposable:
                        await resourceAsyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;

                    default:
                        resource.Dispose();
                        break;
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            await foreach (var message in Messages.WithCancellation(cancellationToken)) {
                if (message is not PersistentSubscriptionMessage.Event(var resolvedEvent, _))
                    continue;

                yield return resolvedEvent;
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            _cts.Dispose();
            _call?.Dispose();
        }

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        internal Task Ack(params Uuid[] eventIds) => AckInternal(eventIds);

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        internal Task Ack(IEnumerable<Uuid> eventIds) => Ack(eventIds.ToArray());

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="resolvedEvents">The <see cref="ResolvedEvent"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        internal Task Ack(params ResolvedEvent[] resolvedEvents) => Ack(Array.ConvertAll(resolvedEvents, resolvedEvent => resolvedEvent.OriginalEvent.EventId));

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="resolvedEvents">The <see cref="ResolvedEvent"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        internal Task Ack(IEnumerable<ResolvedEvent> resolvedEvents) => Ack(resolvedEvents.Select(resolvedEvent => resolvedEvent.OriginalEvent.EventId));

        /// <summary>
        /// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
        /// </summary>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
        /// <param name="reason">A reason given.</param>
        /// <param name="eventIds">The <see cref="Uuid"/> of the <see cref="ResolvedEvent" />s to nak. There should not be more than 2000 to nak at a time.</param>
        /// <exception cref="ArgumentException">The number of eventIds exceeded the limit of 2000.</exception>
        internal Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Uuid[] eventIds) => NackInternal(eventIds, action, reason);

        /// <summary>
        /// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
        /// </summary>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
        /// <param name="reason">A reason given.</param>
        /// <param name="resolvedEvents">The <see cref="ResolvedEvent" />s to nak. There should not be more than 2000 to nak at a time.</param>
        /// <exception cref="ArgumentException">The number of resolvedEvents exceeded the limit of 2000.</exception>
        public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params ResolvedEvent[] resolvedEvents) =>
            Nack(action, reason, Array.ConvertAll(resolvedEvents, re => re.OriginalEvent.EventId));

        static ResolvedEvent ConvertToResolvedEvent(ReadResp response) =>
            new(
                ConvertToEventRecord(response.Event.Event)!,
                ConvertToEventRecord(response.Event.Link),
                response.Event.PositionCase switch {
                    ReadResp.Types.ReadEvent.PositionOneofCase.CommitPosition => response.Event.CommitPosition,
                    _                                                         => null
                }
            );

        Task AckInternal(params Uuid[] eventIds) {
            if (eventIds.Length > MaxEventIdLength)
                throw new ArgumentException(
                    $"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
                    nameof(eventIds)
                );

            return _call is null
                ? throw new InvalidOperationException()
                : _call.RequestStream.WriteAsync(
                    new ReadReq {
                        Ack = new ReadReq.Types.Ack {
                            Ids = {
                                Array.ConvertAll(eventIds, id => id.ToDto())
                            }
                        }
                    }
                );
        }

        Task NackInternal(Uuid[] eventIds, PersistentSubscriptionNakEventAction action, string reason) {
            if (eventIds.Length > MaxEventIdLength)
                throw new ArgumentException(
                    $"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
                    nameof(eventIds)
                );

            return _call is null
                ? throw new InvalidOperationException()
                : _call.RequestStream.WriteAsync(
                    new ReadReq {
                        Nack = new ReadReq.Types.Nack {
                            Ids = {
                                Array.ConvertAll(eventIds, id => id.ToDto())
                            },
                            Action = action switch {
                                PersistentSubscriptionNakEventAction.Park  => ReadReq.Types.Nack.Types.Action.Park,
                                PersistentSubscriptionNakEventAction.Retry => ReadReq.Types.Nack.Types.Action.Retry,
                                PersistentSubscriptionNakEventAction.Skip  => ReadReq.Types.Nack.Types.Action.Skip,
                                PersistentSubscriptionNakEventAction.Stop  => ReadReq.Types.Nack.Types.Action.Stop,
                                _                                          => ReadReq.Types.Nack.Types.Action.Unknown
                            },
                            Reason = reason
                        }
                    }
                );
        }

        static EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
            e is null
                ? null
                : new EventRecord(
                    e.StreamIdentifier!,
                    Uuid.FromDto(e.Id),
                    new StreamPosition(e.StreamRevision),
                    new Position(e.CommitPosition, e.PreparePosition),
                    e.Metadata,
                    e.Data.ToByteArray(),
                    e.CustomMetadata.ToByteArray()
                );
    }
}
