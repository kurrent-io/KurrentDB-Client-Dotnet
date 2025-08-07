// ReSharper disable InconsistentNaming
// ReSharper disable ConvertIfStatementToReturnStatement

using System.Threading.Channels;
using EventStore.Client;
using Grpc.Core;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using KurrentDB.Protocol.PersistentSubscriptions.V1;
using static System.Threading.Channels.Channel;
using static KurrentDB.Protocol.PersistentSubscriptions.V1.ReadResp.ContentOneofCase;

namespace Kurrent.Client.PersistentSubscriptions;

public partial class PersistentSubscriptionsClient {
    /// <summary>
    /// Subscribes to a persistent subscription. Messages must be manually acknowledged
    /// </summary>
    public async ValueTask<Result<PersistentSubscription, SubscribeToStreamError>> SubscribeToStream(
        string streamName,
        string groupName,
        Func<PersistentSubscription, Record, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        int bufferSize = 10,
        CancellationToken cancellationToken = default
    ) {
        try {
            var stream = await SubscribeToStream(
                streamName, groupName, bufferSize,
                cancellationToken
            );

            var subscription = await PersistentSubscription
                .Confirm(
                    stream.Value,
                    eventAppeared,
                    subscriptionDropped ?? delegate { },
                    Logger,
                    cancellationToken
                )
                .ConfigureAwait(false);

            return subscription;
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<PersistentSubscription, SubscribeToStreamError>(
                ex switch {
                    // AccessDeniedException                              => rpcEx.AsAccessDeniedError(),
                    //
                    // PersistentSubscriptionNotFoundException pEx        => rpcEx.AsPersistentSubscriptionNotFoundError(pEx.StreamName, pEx.GroupName),
                    // MaximumSubscribersReachedException pEx             => rpcEx.AsMaximumSubscribersReachedError(pEx.StreamName, pEx.GroupName),
                    // PersistentSubscriptionDroppedByServerException pEx => rpcEx.AsPersistentSubscriptionDroppedError(pEx.StreamName, pEx.GroupName),
                    // _                                                  => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
    }

    /// <summary>
    /// Subscribes to a persistent subscription. Messages must be manually acknowledged.
    /// </summary>
    public async ValueTask<Result<PersistentSubscriptionResult, SubscribeToStreamError>> SubscribeToStream(
        string streamName, string groupName, int bufferSize = 10, CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(streamName);
        ArgumentNullException.ThrowIfNull(groupName);
        ArgumentException.ThrowIfNullOrEmpty(streamName, nameof(streamName));
        ArgumentException.ThrowIfNullOrEmpty(groupName, nameof(groupName));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        try {
            var readOptions = new ReadReq.Types.Options {
                BufferSize = bufferSize,
                GroupName  = groupName,
                UuidOption = new ReadReq.Types.Options.Types.UUIDOption { Structured = new Empty() }
            };

            if (streamName is SystemStreams.AllStream)
                readOptions.All = new Empty();
            else
                readOptions.StreamIdentifier = streamName;

            var request = new ReadReq { Options = readOptions };

            return await ValueTask.FromResult(
                Result.Success<PersistentSubscriptionResult, SubscribeToStreamError>(
                    new PersistentSubscriptionResult(
                        streamName, groupName, GetClient,
                        request, SerializerProvider,
                        MetadataDecoder, cancellationToken
                    )
                )
            );
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<PersistentSubscriptionResult, SubscribeToStreamError>(
                ex switch {
                    // AccessDeniedException                              => rpcEx.AsAccessDeniedError(),
                    //
                    // PersistentSubscriptionNotFoundException pEx        => rpcEx.AsPersistentSubscriptionNotFoundError(pEx.StreamName, pEx.GroupName),
                    // MaximumSubscribersReachedException pEx             => rpcEx.AsMaximumSubscribersReachedError(pEx.StreamName, pEx.GroupName),
                    // PersistentSubscriptionDroppedByServerException pEx => rpcEx.AsPersistentSubscriptionDroppedError(pEx.StreamName, pEx.GroupName),
                    // _                                                  => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }

        async ValueTask<KurrentDB.Protocol.PersistentSubscriptions.V1.PersistentSubscriptions.PersistentSubscriptionsClient> GetClient(CancellationToken ct) {
            if (streamName is not SystemStreams.AllStream) return ServiceClient;
            return ServiceClient;
        }
    }

    /// <summary>
    /// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged
    /// </summary>
    public async ValueTask<Result<PersistentSubscription, SubscribeToAllError>> SubscribeToAll(
        string groupName,
        Func<PersistentSubscription, Record, int?, CancellationToken, Task> eventAppeared,
        Action<PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
        int bufferSize = 10,
        CancellationToken cancellationToken = default
    ) {
        try {
            var subscription = await SubscribeToStream(
                SystemStreams.AllStream, groupName, eventAppeared,
                subscriptionDropped, bufferSize, cancellationToken
            ).ConfigureAwait(false);

            return Result.Success<PersistentSubscription, SubscribeToAllError>(subscription.Value);
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<PersistentSubscription, SubscribeToAllError>(
                ex switch {
                    // AccessDeniedException                              => rpcEx.AsAccessDeniedError(),
                    //
                    // MaximumSubscribersReachedException pEx             => rpcEx.AsMaximumSubscribersReachedError(pEx.StreamName, pEx.GroupName),
                    // PersistentSubscriptionDroppedByServerException pEx => rpcEx.AsPersistentSubscriptionDroppedError(pEx.StreamName, pEx.GroupName),
                    // _                                                  => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
    }

    /// <summary>
    /// Subscribes to a persistent subscription to $all. Messages must be manually acknowledged.
    /// </summary>
    /// <param name="groupName">The name of the persistent subscription group.</param>
    /// <param name="bufferSize">The size of the buffer.</param>
    /// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
    /// <returns></returns>
    public async ValueTask<Result<PersistentSubscriptionResult, SubscribeToAllError>> SubscribeToAll(
        string groupName, int bufferSize = 10, CancellationToken cancellationToken = default
    ) {
        try {
            var subscription = await SubscribeToStream(
                SystemStreams.AllStream, groupName, bufferSize,
                cancellationToken
            );

            return await ValueTask.FromResult(Result.Success<PersistentSubscriptionResult, SubscribeToAllError>(subscription.Value));
        }
        catch (Exception ex) when (ex.InnerException is RpcException rpcEx) {
            return Result.Failure<PersistentSubscriptionResult, SubscribeToAllError>(
                ex switch {
                    // AccessDeniedException                              => rpcEx.AsAccessDeniedError(),
                    // MaximumSubscribersReachedException pEx             => rpcEx.AsMaximumSubscribersReachedError(pEx.StreamName, pEx.GroupName),
                    // PersistentSubscriptionDroppedByServerException pEx => rpcEx.AsPersistentSubscriptionDroppedError(pEx.StreamName, pEx.GroupName),
                    // _                                                  => throw KurrentException.CreateUnknown(nameof(DeleteToStream), ex)
                }
            );
        }
    }

    public class PersistentSubscriptionResult : IAsyncEnumerable<Record>, IAsyncDisposable, IDisposable {
        const int MaxEventIdLength = 2000;

        static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(1) {
            SingleReader                  = true,
            SingleWriter                  = true,
            AllowSynchronousContinuations = true
        };

        readonly CancellationTokenSource Cancellator;

        readonly Channel<PersistentSubscriptionMessage> Channel;
        readonly IMetadataDecoder                       MetadataDecoder;
        readonly ReadReq                                Request;
        readonly ISchemaSerializerProvider              SerializerProvider;

        int MessagesEnumerated;

        AsyncDuplexStreamingCall<ReadReq, ReadResp>? ReadCall;

        internal PersistentSubscriptionResult(
            string streamName,
            string groupName,
            Func<CancellationToken, ValueTask<KurrentDB.Protocol.PersistentSubscriptions.V1.PersistentSubscriptions.PersistentSubscriptionsClient>> getClient,
            ReadReq request,
            ISchemaSerializerProvider serializationProvider,
            IMetadataDecoder metadataDecoder,
            CancellationToken cancellationToken
        ) {
            StreamName         = streamName;
            GroupName          = groupName;
            SerializerProvider = serializationProvider;
            MetadataDecoder    = metadataDecoder;

            Request = request;

            Channel = CreateBounded<PersistentSubscriptionMessage>(ReadBoundedChannelOptions);

            Cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = PumpMessages();

            return;

            async Task PumpMessages() {
                try {
                    var client = await getClient(Cancellator.Token).ConfigureAwait(false);

                    ReadCall = client.Read(cancellationToken: cancellationToken);

                    await ReadCall.RequestStream.WriteAsync(Request).ConfigureAwait(false);

                    await foreach (var response in ReadCall.ResponseStream.ReadAllAsync(Cancellator.Token).ConfigureAwait(false)) {
                        PersistentSubscriptionMessage subscriptionMessage = response.ContentCase switch {
                            SubscriptionConfirmation => new PersistentSubscriptionMessage.SubscriptionConfirmation(
                                response.SubscriptionConfirmation.SubscriptionId
                            ),
                            Event => new PersistentSubscriptionMessage.Event(
                                await response.Event.MapToRecord(SerializerProvider, MetadataDecoder, ct: cancellationToken),
                                response.Event.CountCase switch {
                                    ReadResp.Types.ReadEvent.CountOneofCase.RetryCount => response.Event.RetryCount,
                                    _                                                  => null
                                }
                            ),
                            _ => PersistentSubscriptionMessage.Unknown.Instance
                        };

                        // TODO WC: tracing
                        // if (subscriptionMessage is PersistentSubscriptionMessage.Event evnt)
                        // 	KurrentDBClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
                        // 		SubscriptionId,
                        // 		evnt.ResolvedEvent,
                        // 		channelInfo,
                        // 		settings,
                        // 		userCredentials
                        // 	);

                        await Channel.Writer.WriteAsync(subscriptionMessage, Cancellator.Token).ConfigureAwait(false);
                    }

                    Channel.Writer.TryComplete();
                }
                catch (Exception ex) {
                    if (ex is PersistentSubscriptionNotFoundException) {
                        await Channel.Writer
                            .WriteAsync(PersistentSubscriptionMessage.NotFound.Instance, cancellationToken)
                            .ConfigureAwait(false);

                        Channel.Writer.TryComplete();
                        return;
                    }

                    Channel.Writer.TryComplete(ex);
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
                if (Interlocked.Exchange(ref MessagesEnumerated, 1) == 1)
                    throw new InvalidOperationException("Messages may only be enumerated once.");

                return GetMessages();

                async IAsyncEnumerable<PersistentSubscriptionMessage> GetMessages() {
                    try {
                        await foreach (var message in Channel.Reader.ReadAllAsync(Cancellator.Token)) {
                            if (message is PersistentSubscriptionMessage.SubscriptionConfirmation(var subscriptionId))
                                SubscriptionId = subscriptionId;

                            yield return message;
                        }
                    }
                    finally {
                        Cancellator.Cancel();
                    }
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync() {
            await CastAndDispose(Cancellator).ConfigureAwait(false);
            await CastAndDispose(ReadCall).ConfigureAwait(false);

            return;

            static async ValueTask CastAndDispose(IDisposable? resource) {
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
        public async IAsyncEnumerator<Record> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            await foreach (var message in Messages.WithCancellation(cancellationToken)) {
                if (message is not PersistentSubscriptionMessage.Event(var record, _))
                    continue;

                yield return record;
            }
        }

        /// <inheritdoc />
        public void Dispose() {
            Cancellator.Dispose();
            ReadCall?.Dispose();
        }

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="eventIds">The <see cref="Guid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        public Task Ack(params Guid[] eventIds) => AckInternal(eventIds);

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="eventIds">The <see cref="Guid"/> of the <see cref="ResolvedEvent" />s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        public Task Ack(IEnumerable<Guid> eventIds) => Ack(eventIds.ToArray());

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="records">The <see cref="Record"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        public Task Ack(params Record[] records) => Ack(Array.ConvertAll(records, record => record.Id));

        /// <summary>
        /// Acknowledge that a message has completed processing (this will tell the server it has been processed).
        /// </summary>
        /// <remarks>There is no need to ack a message if you have Auto Ack enabled.</remarks>
        /// <param name="records">The <see cref="Record"></see>s to acknowledge. There should not be more than 2000 to ack at a time.</param>
        public Task Ack(IEnumerable<Record> records) => Ack(records.Select(record => record.Id));

        /// <summary>
        /// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
        /// </summary>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
        /// <param name="reason">A reason given.</param>
        /// <param name="eventIds">The <see cref="Guid"/> of the <see cref="Record" />s to nak. There should not be more than 2000 to nak at a time.</param>
        /// <exception cref="ArgumentException">The number of eventIds exceeded the limit of 2000.</exception>
        public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Guid[] eventIds) => NackInternal(eventIds, action, reason);

        /// <summary>
        /// Acknowledge that a message has failed processing (this will tell the server it has not been processed).
        /// </summary>
        /// <param name="action">The <see cref="PersistentSubscriptionNakEventAction"/> to take.</param>
        /// <param name="reason">A reason given.</param>
        /// <param name="records">The <see cref="Record" />s to nak. There should not be more than 2000 to nak at a time.</param>
        /// <exception cref="ArgumentException">The number of resolvedEvents exceeded the limit of 2000.</exception>
        public Task Nack(PersistentSubscriptionNakEventAction action, string reason, params Record[] records) =>
            Nack(action, reason, Array.ConvertAll(records, record => record.Id));

        Task AckInternal(params Guid[] eventIds) {
            if (eventIds.Length > MaxEventIdLength)
                throw new ArgumentException(
                    $"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
                    nameof(eventIds)
                );

            return ReadCall is null
                ? throw new InvalidOperationException()
                : ReadCall.RequestStream.WriteAsync(
                    new ReadReq {
                        Ack = new ReadReq.Types.Ack {
                            Ids = {
                                Array.ConvertAll(eventIds, id => Uuid.FromGuid(id).ToDto())
                            }
                        }
                    }
                );
        }

        Task NackInternal(Guid[] eventIds, PersistentSubscriptionNakEventAction action, string reason) {
            if (eventIds.Length > MaxEventIdLength)
                throw new ArgumentException(
                    $"The number of eventIds exceeds the maximum length of {MaxEventIdLength}.",
                    nameof(eventIds)
                );

            return ReadCall is null
                ? throw new InvalidOperationException()
                : ReadCall.RequestStream.WriteAsync(
                    new ReadReq {
                        Nack = new ReadReq.Types.Nack {
                            Ids = {
                                Array.ConvertAll(eventIds, id => Uuid.FromGuid(id).ToDto())
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
    }
}
