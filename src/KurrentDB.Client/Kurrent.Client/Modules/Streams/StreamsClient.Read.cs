#pragma warning disable CS8509

using System.Threading.Channels;
using KurrentDB.Protocol.Streams.V1;
using Grpc.Core;
using KurrentDB.Client;
using static Kurrent.Client.ErrorDetails;
using static KurrentDB.Protocol.Streams.V1.ReadResp.ContentOneofCase;
using static Kurrent.Client.Streams.StreamsClientV1Mapper;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    // internal async IAsyncEnumerable<ReadMessage> NewRead(
    //     LogPosition startPosition,
    //     long limit,
    //     ReadFilter? filter = null,
    //     ReadDirection direction = ReadDirection.Forwards,
    //     HeartbeatOptions? heartbeatOptions = null,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var channel = Channel.CreateUnbounded<ReadMessage>(new() {
    //         SingleReader = true,
    //         SingleWriter = true
    //     });
    //
    //     var request = new Contracts.ReadRequest {
    //         Filter        = filter?.Map(),
    //         StartPosition = startPosition,
    //         Limit         = limit,
    //         Direction     = direction.Map(),
    //         Heartbeats    = heartbeatOptions?.Map()
    //     };
    //
    //     // Start a task to read from gRPC and write to the channel
    //     using var readTask = Task.Run(
    //         async () => {
    //             try {
    //                 using var session = ServiceClient.ReadSession(request, cancellationToken: cancellationToken);
    //
    //                 while (await session.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false)) {
    //                     var response = session.ResponseStream.Current;
    //
    //                     await (response.ResultCase switch {
    //                         Contracts.ReadResponse.ResultOneofCase.Success   => HandleSuccess(response),
    //                         Contracts.ReadResponse.ResultOneofCase.Heartbeat => HandleHeartbeat(response),
    //                         Contracts.ReadResponse.ResultOneofCase.Failure   => HandleFailure(response),
    //                         _                                                => throw KurrentClientException.CreateUnknown(
    //                             nameof(Read), new UnreachableException($"Unreachable error while reading stream: {response.ResultCase}"))
    //                     });
    //                 }
    //
    //                 channel.Writer.TryComplete();
    //             }
    //             catch (Exception ex) {
    //                 channel.Writer.Complete(new Exception($"Error while reading stream: {ex.Message}", ex));
    //             }
    //         },
    //         cancellationToken
    //     );
    //
    //     await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
    //         yield return result;
    //
    //     yield break;
    //
    //     async ValueTask HandleSuccess(Contracts.ReadResponse response) {
    //         foreach (var record in response.Success.Records) {
    //             var mappedRecord = await record.Map(SerializerProvider, cancellationToken).ConfigureAwait(false);
    //             await channel.Writer.WriteAsync(mappedRecord, cancellationToken).ConfigureAwait(false);
    //         }
    //     }
    //
    //     ValueTask HandleHeartbeat(Contracts.ReadResponse response) =>
    //          channel.Writer.WriteAsync(response.Heartbeat.Map(), cancellationToken);
    //
    //     ValueTask HandleFailure(Contracts.ReadResponse response) {
    //         IResultError err = response.Failure.ErrorCase switch {
    //             Contracts.ReadFailure.ErrorOneofCase.AccessDenied   => new AccessDenied(),
    //             Contracts.ReadFailure.ErrorOneofCase.StreamDeleted  => new StreamDeleted(x => x.With("stream",response.Failure.StreamDeleted.Stream)),
    //             Contracts.ReadFailure.ErrorOneofCase.StreamNotFound => new StreamNotFound(x => x.With("stream",response.Failure.StreamDeleted.Stream)),
    //         };
    //
    //         channel.Writer.TryComplete(new Exception($"Read operation failed: {err}"));
    //
    //         return ValueTask.CompletedTask;
    //     }
    // }

    // public IAsyncEnumerable<ReadMessage> Read(
    //     LogPosition startPosition, long limit,
    //     ReadFilter filter, Direction direction,
    //     HeartbeatOptions heartbeatOptions,
    //     CancellationToken cancellationToken = default
    // ) {
    //     var session = filter.IsStreamNameFilter
    //         ? ReadStream(
    //             filter.Expression, startPosition,
    //             limit, direction, cancellationToken
    //         )
    //         : ReadAll(
    //             startPosition, limit, direction, filter,
    //             heartbeatOptions, cancellationToken
    //         );
    //
    //     return session;
    // }

    // public async IAsyncEnumerable<ReadMessage> ReadAll(
    //     LogPosition startPosition, long limit, ReadDirection direction,
    //     ReadFilter filter, HeartbeatOptions heartbeatOptions,
    //     [EnumeratorCancellation] CancellationToken cancellationToken = default
    // ) {
    //     var legacyPosition = startPosition.ConvertToLegacyPosition();
    //     var eventFilter    = filter.ConvertToEventFilter(heartbeatOptions.RecordsThreshold);
    //
    //     var session = LegacyClient.ReadAllAsync(
    //         direction, legacyPosition, eventFilter, limit,
    //         cancellationToken: cancellationToken
    //     );
    //
    //     // what about checkpoints (aka heartbeats), only with new protocol?
    //     await foreach (var re in session.ConfigureAwait(false)) {
    //         var record = await LegacyConverter
    //             .ConvertToRecord(re, cancellationToken)
    //             .ConfigureAwait(false);
    //
    //         yield return record;
    //     }
    // }

    // public async ValueTask<Result<Messages, ReadError>> ReadAll(ReadAllOptions options) {
    //     options.EnsureValid();
    //
    //     var legacyOptions = (
    //         Direction : (Direction)options.Direction,
    //         Position  : options.Start.ConvertToLegacyPosition(),
    //         Filter    : options.Filter.ConvertToEventFilter(options.Heartbeat.RecordsThreshold),
    //         MaxCount  : options.Limit
    //     );
    //
    //     // so to check AccessDenied, I must call the api. However, once I call it, it will throw an exception,
    //     // and if it doesn't, I must now do another call because I cannot continue reading... cause InvalidOperationException("Messages may only be enumerated once."))
    //     // need to get read v2 immediately
    //     try {
    //         await LegacyClient
    //             .ReadAllAsync(Direction.Forwards, Position.Start, 1, cancellationToken: options.CancellationToken).Messages
    //             .AnyAsync().ConfigureAwait(false);
    //     } catch (AccessDeniedException) {
    //         return new ReadError(new ErrorDetails.AccessDenied(x => x.With("reason", "Access denied while reading all streams.")));
    //     }
    //     catch (Exception ex) when (ex is not KurrentClientException) {
    //         throw KurrentClientException.CreateUnknown(nameof(ReadAll), ex);
    //     }
    //
    //     var messages =  LegacyClient.ReadAllAsync(
    //         legacyOptions.Direction,
    //         legacyOptions.Position,
    //         legacyOptions.Filter,
    //         legacyOptions.MaxCount,
    //         cancellationToken: options.CancellationToken
    //     ).Messages;
    //
    //     var source = messages.SelectAwait<StreamMessage, ReadMessage>(async se => se switch {
    //         StreamMessage.Event message         => await LegacyConverter.ConvertToRecord(message.ResolvedEvent, options.CancellationToken).ConfigureAwait(false),
    //         StreamMessage.CaughtUp caughtUp     => Heartbeat.CreateCaughtUp(caughtUp.Position.ConvertToLogPosition(), caughtUp.Timestamp),
    //         StreamMessage.FellBehind fellBehind => Heartbeat.CreateFellBehind(fellBehind.Position.ConvertToLogPosition(), fellBehind.Timestamp),
    //     });
    //
    //     var channel = Channel.CreateBounded<ReadMessage>(new BoundedChannelOptions(100));
    //     return new Messages(channel);
    // }

    // public async ValueTask<Result<Messages, ReadError>> ReadAll(ReadAllOptions options) {
    //     options.EnsureValid();
    //
    //     ReadReq request = StreamsV1Mapper.CreateReadRequest(options);
    //
    //
    //
    //
    //     var legacyOptions = (
    //         Direction : (Direction)options.Direction,
    //         Position  : options.Start.ConvertToLegacyPosition(),
    //         Filter    : options.Filter.ConvertToEventFilter(options.Heartbeat.RecordsThreshold),
    //         MaxCount  : options.Limit
    //     );
    //
    //     // so to check AccessDenied, I must call the api. However, once I call it, it will throw an exception,
    //     // and if it doesn't, I must now do another call because I cannot continue reading... cause InvalidOperationException("Messages may only be enumerated once."))
    //     // need to get read v2 immediately
    //     try {
    //         await LegacyClient
    //             .ReadAllAsync(Direction.Forwards, Position.Start, 1, cancellationToken: options.CancellationToken).Messages
    //             .AnyAsync().ConfigureAwait(false);
    //     } catch (AccessDeniedException) {
    //         return new ReadError(new ErrorDetails.AccessDenied(x => x.With("reason", "Access denied while reading all streams.")));
    //     }
    //     catch (Exception ex) when (ex is not KurrentClientException) {
    //         throw KurrentClientException.CreateUnknown(nameof(ReadAll), ex);
    //     }
    //
    //     var messages =  LegacyClient.ReadAllAsync(
    //         legacyOptions.Direction,
    //         legacyOptions.Position,
    //         legacyOptions.Filter,
    //         legacyOptions.MaxCount,
    //         cancellationToken: options.CancellationToken
    //     ).Messages;
    //
    //     var source = messages.SelectAwait<StreamMessage, ReadMessage>(async se => se switch {
    //         StreamMessage.Event message         => await LegacyConverter.ConvertToRecord(message.ResolvedEvent, options.CancellationToken).ConfigureAwait(false),
    //         StreamMessage.CaughtUp caughtUp     => Heartbeat.CreateCaughtUp(caughtUp.Position.ConvertToLogPosition(), caughtUp.Timestamp),
    //         StreamMessage.FellBehind fellBehind => Heartbeat.CreateFellBehind(fellBehind.Position.ConvertToLogPosition(), fellBehind.Timestamp),
    //     });
    //
    //     var channel = Channel.CreateBounded<ReadMessage>(new BoundedChannelOptions(100));
    //
    //     return new Messages(channel);
    // }
    //
    // public async ValueTask<Result<Messages, ReadError>> ReadStream(ReadStreamOptions options) {
    //     options.EnsureValid();
    //
    //     var session = LegacyClient.ReadStreamAsync(
    //         (Direction) options.Direction, options.Stream,
    //         options.Start.ConvertToLegacyStreamPosition(),
    //         options.Limit,
    //         cancellationToken: options.CancellationToken
    //     );
    //
    //     try {
    //         if (await session.ReadState == ReadState.StreamNotFound)
    //             return new ReadError(new StreamNotFound(x => x.With("stream", options.Stream)));
    //     }
    //     catch (AccessDeniedException) {
    //         return new ReadError(new ErrorDetails.AccessDenied(x => x.With("stream", options.Stream)));
    //     }
    //     catch (Exception ex) when (ex is not KurrentClientException) {
    //         throw KurrentClientException.CreateUnknown(nameof(ReadStream), ex);
    //     }
    //
    //     var source = session.Messages
    //         .Where(se => se is StreamMessage.Event or StreamMessage.CaughtUp)
    //         .SelectAwait<StreamMessage, ReadMessage>(async se => se switch {
    //             StreamMessage.Event message     => await LegacyConverter.ConvertToRecord(message.ResolvedEvent, options.CancellationToken).ConfigureAwait(false),
    //             StreamMessage.CaughtUp caughtUp => Heartbeat.CreateCaughtUp(caughtUp.Position.ConvertToLogPosition(), caughtUp.Timestamp)
    //         });
    //
    //     var channel = Channel.CreateBounded<ReadMessage>(new BoundedChannelOptions(100));
    //     return new Messages(channel);
    // }

    public async IAsyncEnumerable<Record> ReadAllAsync(ReadAllOptions options, Func<Heartbeat, ValueTask> onHeartbeat) {
        await using var messages = await ReadAll(options).ThrowOnFailureAsync();
        await foreach (var msg in messages) {
            if (msg.IsRecord) {
                yield return msg.AsRecord;
                continue;
            }

            await onHeartbeat(msg.AsHeartbeat).ConfigureAwait(false);
        }
    }

    public async IAsyncEnumerable<Record> ReadAllAsync(ReadAllOptions options) {
        await using var messages = await ReadAll(options).ThrowOnFailureAsync();
        await foreach (var msg in messages) {
            if (!msg.IsRecord) continue;
            yield return msg.AsRecord;
        }
    }

    public async IAsyncEnumerable<Record> ReadStreamAsync(ReadStreamOptions options) {
        await using var messages = await ReadStream(options).ThrowOnFailureAsync();
        await foreach (var msg in messages) {
            if (msg.IsRecord)
                yield return msg.AsRecord;
        }
    }

    public ValueTask<Result<Messages, ReadError>> ReadStream(StreamName stream, CancellationToken cancellationToken = default) =>
        ReadStream(new ReadStreamOptions { Stream = stream, CancellationToken = cancellationToken });

    public ValueTask<Result<Messages, ReadError>> ReadAll(ReadAllOptions options) =>
        ReadCore(Requests.CreateReadRequest(options), options.BufferSize, options.Timeout, options.SkipDecoding, options.CancellationToken);

    public ValueTask<Result<Messages, ReadError>> ReadStream(ReadStreamOptions options) =>
        ReadCore(Requests.CreateReadRequest(options), options.BufferSize, options.Timeout, options.SkipDecoding, options.CancellationToken);

    async ValueTask<Result<Messages, ReadError>> ReadCore(ReadReq request, int bufferSize, TimeSpan timeout, bool skipDecoding, CancellationToken cancellationToken) {
        var cancellator   = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var stoppingToken = cancellator.Token;

        var session = LegacyServiceClient.Read(request, cancellationToken: stoppingToken);

        // Check for access denied. the legacy exception mapper throws this exception...
        // we could skip it for this operation... requires refactoring the interceptor
        // we also need to check for stream not found. this is absurd...
        try {
            await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false);

            if (session.ResponseStream.Current.ContentCase is ReadResp.ContentOneofCase.StreamNotFound) {
                cancellator.Dispose();

                // we must check the metadata stream to see if the stream was deleted.
                // this is just a big brutal hack because:
                // - what happens if the meta-stream is deleted in the meantime?
                // - or we lose access to it?
                // it's like inception but worst...
                StreamName stream = request.Options.Stream.StreamIdentifier.StreamName.ToStringUtf8();

                // and one must break out right here if the stream is a meta-stream cause otherwise
                // we get into a logical loop and boom - stack overflow
                // this is the most absurd thing ever
                if (stream.IsMetastream)
                    return Result.Failure<Messages, ReadError>(new StreamNotFound(mt => mt.With("stream", stream)));

                // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                // because we are checking the metadata stream on a failure path,
                // we must use the original cancellation token
                return await this.ReadLastStreamRecord(SystemStreams.MetastreamOf(stream), cancellationToken)
                    .MatchAsync(
                        rec => {
                            if (rec == Record.None)
                                return Result.Failure<Messages, ReadError>(new StreamNotFound(mt => mt.With("stream", stream)));

                            var metadata = (StreamMetadata)rec.Value!;
                            if (metadata.TruncateBefore == StreamRevision.Max)
                                return Result.Failure<Messages, ReadError>(new StreamDeleted(mt => mt.With("stream", stream)));

                            throw KurrentException.CreateUnknown(nameof(ReadCore),
                                new InvalidOperationException($"Stream {stream} was not found, but metadata does exist and the stream was not truncated. This is unexpected."));
                        },
                        err => err.Case switch {
                            ReadError.ReadErrorCase.StreamDeleted  => Result.Failure<Messages, ReadError>(new StreamDeleted(mt => mt.With("stream", stream))),
                            ReadError.ReadErrorCase.StreamNotFound => Result.Failure<Messages, ReadError>(new StreamNotFound(mt => mt.With("stream", stream))),
                            ReadError.ReadErrorCase.AccessDenied   => Result.Failure<Messages, ReadError>(new AccessDenied(mt => mt.With("stream", stream)))
                        }
                    )
                    .ConfigureAwait(false);
            }

        }
        catch (AccessDeniedException) {
            cancellator.Dispose();
            return Result.Failure<Messages, ReadError>(new AccessDenied(mt => mt
                .With("stream", request.Options.Stream.StreamIdentifier.StreamName.ToStringUtf8())));
        }
        catch (StreamDeletedException) {
            cancellator.Dispose();
            return Result.Failure<Messages, ReadError>(new StreamTombstoned(mt => mt
                .With("stream", request.Options.Stream.StreamIdentifier.StreamName.ToStringUtf8())));
        }

        // Creates a factory function instead of starting immediately
        var channelFactory = new Func<Channel<ReadMessage>>(() => StartReadMessageRelay(session, bufferSize, timeout, skipDecoding, cancellator));

        return new Messages(channelFactory);
    }

    /// <summary>
    /// Initiates a message relay process to supply messages via a channel.
    /// </summary>
    /// <param name="session">An asynchronous server streaming call providing subscription responses.</param>
    /// <param name="bufferSize">The maximum number of messages the channel can buffer before applying backpressure.</param>
    /// <param name="consumeTimeout">The maximum time to wait for a subscription message before timing out.</param>
    /// <param name="skipDecoding"></param>
    /// <param name="cancellator">A cancellation token source used to signal termination of the message relay process.</param>
    /// <returns>A bounded channel that streams subscription messages.</returns>
    Channel<ReadMessage> StartReadMessageRelay(AsyncServerStreamingCall<ReadResp> session, int bufferSize, TimeSpan consumeTimeout, bool skipDecoding, CancellationTokenSource cancellator) {
        // create a bounded channel for backpressure control
        var channel = Channel.CreateBounded<ReadMessage>(
            new BoundedChannelOptions(bufferSize) {
                FullMode     = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            }
        );

        var stoppingToken = cancellator.Token;

        // start the background task to pump the messages through the channel
        _ = Task.Run(
            async () => {
                try {
                    // must continue from the freaking cursor because we had to try to read
                    // the first entry to check if the stream existed or if access was granted
                    do {
                        var resp = session.ResponseStream.Current;

                        if (resp.ContentCase is not (Event or Checkpoint or CaughtUp or FellBehind)) continue;

                        ReadMessage message = resp.ContentCase switch {
                            Event      => await resp.Event.MapToRecord(SerializerProvider, MetadataDecoder, skipDecoding, stoppingToken).ConfigureAwait(false),
                            Checkpoint => resp.Checkpoint.MapToHeartbeat(),
                            CaughtUp   => resp.CaughtUp.MapToHeartbeat(),
                            FellBehind => resp.FellBehind.MapToHeartbeat(),
                            _          => throw new InvalidOperationException($"Unreachable error while reading stream. {resp.ContentCase}")
                        };

                        // try to write immediately without blocking
                        if (channel.Writer.TryWrite(message))
                            continue; // no timeout needed

                        // the channel is full, we must add timeout protection
                        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)
                            .With(x => x.CancelAfter(consumeTimeout));

                        await channel.Writer
                            .WriteAsync(message, timeout.Token)
                            .ConfigureAwait(false);
                    } while (await session.ResponseStream.MoveNext(stoppingToken).ConfigureAwait(false));

                    channel.Writer.TryComplete();
                }  catch (OperationCanceledException ex) when (ex.CancellationToken != stoppingToken) {
                    channel.Writer.TryComplete(new TimeoutException($"Timed out after {consumeTimeout}. The application may not be reading messages fast enough."));
                } catch (OperationCanceledException) {
                    channel.Writer.TryComplete();
                } catch (Exception ex) {
                    channel.Writer.TryComplete(ex);
                } finally {
                    session.Dispose();
                    cancellator.Dispose();
                }
            }, stoppingToken
        );

        return channel;
    }

    public async ValueTask<Result<Record, InspectRecordError>> InspectRecord(LogPosition position, CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfEqual(position, LogPosition.Unset, nameof(position));

        var readResult = await ReadCore(
                request: Requests.CreateInspectRecordRequest(position),
                bufferSize: 1,
                timeout: Timeout.InfiniteTimeSpan,
                skipDecoding: true,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (readResult.IsFailure)
            return Result.Failure<Record, InspectRecordError>(readResult.Error.AsAccessDenied);

        await using var messages = readResult.Value;

        var record = await messages
            .Select(msg => msg.AsRecord)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record ?? Result.Failure<Record, InspectRecordError>(
            new LogPositionNotFound(mt => mt.With("position", position)));
    }
}
