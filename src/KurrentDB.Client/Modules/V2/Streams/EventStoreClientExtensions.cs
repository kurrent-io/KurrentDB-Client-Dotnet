// using System.Threading.Channels;
// using EventStore.Client;
// using Kurrent.Surge.Consumers;
// using Grpc.Core;
// using JetBrains.Annotations;
// using KurrentDB.Client;
// using KurrentDB.Client.Model;
// using Polly;
//
// namespace Kurrent.Surge.Readers;
//
// [PublicAPI]
// public static class ReadAllExtensions {
//     public static Task ReadToChannel(
//         this KurrentDBClient client,
//         Direction direction,
//         Position position,
//         bool resolveLinkTos,
//         long maxCount,
//         ConsumeFilter consumeFilter,
//         Channel<ResolvedEvent> channel,
//         ResiliencePipeline retryPolicy,
//         CancellationToken cancellationToken
//     ) {
//         _ = Task.Run(
//             async () => {
//                 using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//
//                 var filter     = consumeFilter.ToEventFilter();
//                 var checkpoint = position;
//                 var count      = 0;
//
//                 try {
//                     await retryPolicy.ExecuteAsync(
//                         async token => {
//                             var endReached = false;
//
//                             while (!endReached && !token.IsCancellationRequested) {
//                                 var result = client.ReadAllAsync(
//                                     direction: direction,
//                                     position: checkpoint,
//                                     eventFilter: filter,
//                                     maxCount: maxCount,
//                                     resolveLinkTos: resolveLinkTos,
//                                     deadline: TimeSpan.FromMinutes(10),
//                                     cancellationToken: token
//                                 );
//
//                                 await foreach (var msg in result.Messages.WithCancellation(token)) {
//                                     if (token.IsCancellationRequested)
//                                         break;
//
//                                     if (msg is StreamMessage.LastAllStreamPosition) {
//                                         endReached = true;
//                                         break;
//                                     }
//
//                                     if (msg is not StreamMessage.Event evt)
//                                         continue;
//
//                                     checkpoint = evt.ResolvedEvent.OriginalPosition.GetValueOrDefault();
//                                     await channel.Writer.WriteAsync(evt.ResolvedEvent, token);
//
//                                     if (++count == maxCount) {
//                                         endReached = true;
//                                         await cancellator.CancelAsync();
//                                         break;
//                                     }
//                                 }
//                             }
//                         },
//                         cancellator.Token
//                     );
//
//                     channel.Writer.Complete();
//                 }
//                 catch (OperationCanceledException) {
//                     channel.Writer.Complete();
//                 }
//                 // shouldn't this be mapped in the es client to a OperationCanceledException?
//                 // or perhaps simply stop streaming
//                 catch (RpcException rex) when (rex.StatusCode == StatusCode.Cancelled) {
//                     channel.Writer.Complete();
//                 }
//                 catch (Exception ex) {
//                     channel.Writer.Complete(ex);
//                 }
//             },
//             cancellationToken
//         );
//
//         return Task.CompletedTask;
//     }
//
//     public static Task ReadStreamToChannel(
//         this EventStoreClient client,
//         string streamName,
//         StreamPosition position,
//         Direction direction,
//         bool resolveLinkTos,
//         long maxCount,
//         Channel<ResolvedEvent> channel,
//         ResiliencePipeline retryPolicy,
//         CancellationToken cancellationToken
//     ) {
//         _ = Task.Run(
//             async () => {
//                 using var cancellator = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
//
//                 var checkpoint = position;
//                 var count      = 0;
//
//                 try {
//                     await retryPolicy.ExecuteAsync(
//                         async token => {
//                             var endReached = false;
//
//                             while (!endReached && !token.IsCancellationRequested) {
//                                 var result = client.ReadStreamAsync(
//                                     direction: direction,
//                                     streamName: streamName,
//                                     revision: checkpoint,
//                                     maxCount: maxCount,
//                                     resolveLinkTos: resolveLinkTos,
//                                     deadline: TimeSpan.FromMinutes(10),
//                                     cancellationToken: token
//                                 );
//
//                                 var readState = await result.ReadState;
//
//                                 if (readState == ReadState.StreamNotFound) {
//                                     throw new StreamNotFoundError(streamName);
//                                 }
//
//                                 await foreach (var msg in result.Messages.WithCancellation(token)) {
//                                     if (msg is StreamMessage.LastStreamPosition || direction == Direction.Backwards && msg is StreamMessage.FirstStreamPosition) {
//                                         endReached = true;
//                                         break;
//                                     }
//
//                                     if (msg is not StreamMessage.Event evt)
//                                         continue;
//
//                                     checkpoint = evt.ResolvedEvent.OriginalEventNumber;
//                                     await channel.Writer.WriteAsync(evt.ResolvedEvent, token);
//
//                                     if (++count == maxCount) {
//                                         endReached = true;
//                                         await cancellator.CancelAsync();
//                                         break;
//                                     }
//                                 }
//                             }
//                         },
//                         cancellationToken
//                     );
//
//                     channel.Writer.Complete();
//                 }
//                 catch (StreamNotFoundError) {
//                     channel.Writer.Complete();
//                     throw;
//                 }
//                 catch (OperationCanceledException) {
//                     channel.Writer.Complete();
//                 }
//                 // shouldn't this be mapped in the es client to a OperationCanceledException?
//                 // or perhaps simply stop streaming
//                 catch (RpcException rex) when (rex.StatusCode == StatusCode.Cancelled) {
//                     channel.Writer.Complete();
//                 }
//                 catch (Exception ex) {
//                     channel.Writer.Complete(ex);
//                 }
//             },
//             cancellationToken
//         );
//
//         return Task.CompletedTask;
//     }
// }
