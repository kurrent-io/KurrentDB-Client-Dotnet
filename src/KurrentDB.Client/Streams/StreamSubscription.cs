using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client {
	/// <summary>
	/// A class representing a <see cref="StreamSubscription"/>.
	/// </summary>
	public class StreamSubscription : IDisposable {
		private readonly KurrentDBClient.StreamSubscriptionResult                          _subscription;
		private readonly IAsyncEnumerator<StreamMessage>                                    _messages;
		private readonly Func<StreamSubscription, ResolvedEvent, CancellationToken, Task>   _eventAppeared;
		private readonly Func<StreamSubscription, Position, CancellationToken, Task>        _checkpointReached;
		private readonly Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? _subscriptionDropped;
		private readonly ILogger                                                            _log;
		private readonly CancellationTokenSource                                            _cts;
		private          int                                                                _subscriptionDroppedInvoked;

		/// <summary>
		/// The id of the <see cref="StreamSubscription"/> set by the server.
		/// </summary>
		public string SubscriptionId { get; }

		internal static async Task<StreamSubscription> Confirm(
			KurrentDBClient.StreamSubscriptionResult subscription,
			SubscriptionListener subscriptionListener,
			ILogger log,
			CancellationToken cancellationToken = default
		) {
			var messages = subscription.Messages;

			var enumerator = messages.GetAsyncEnumerator(cancellationToken);
			if (!await enumerator.MoveNextAsync().ConfigureAwait(false) ||
			    enumerator.Current is not StreamMessage.SubscriptionConfirmation(var subscriptionId)) {
				throw new InvalidOperationException($"Subscription to {enumerator} could not be confirmed.");
			}

			return new StreamSubscription(
				subscription,
				enumerator,
				subscriptionId,
				subscriptionListener,
				log,
				cancellationToken
			);
		}

		private StreamSubscription(
			KurrentDBClient.StreamSubscriptionResult subscription,
			IAsyncEnumerator<StreamMessage> messages, 
			string subscriptionId,
			SubscriptionListener subscriptionListener,
			ILogger log,
			CancellationToken cancellationToken = default
		) {
			_cts                        = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_subscription               = subscription;
			_messages                   = messages;
			_eventAppeared              = subscriptionListener.EventAppeared;
			_checkpointReached          = subscriptionListener.CheckpointReached ?? ((_, _, _) => Task.CompletedTask);
			_subscriptionDropped        = subscriptionListener.SubscriptionDropped;
			_log                        = log;
			_subscriptionDroppedInvoked = 0;
			SubscriptionId              = subscriptionId;

			_log.LogDebug("Subscription {subscriptionId} confirmed.", SubscriptionId);

			Task.Run(Subscribe, cancellationToken);
		}

		private async Task Subscribe() {
			using var _ = _cts;

			try {
				while (await _messages.MoveNextAsync().ConfigureAwait(false)) {
					var message = _messages.Current;
					try {
						switch (message) {
							case StreamMessage.Event(var resolvedEvent):
								_log.LogTrace(
									"Subscription {subscriptionId} received event {streamName}@{streamRevision} {position}",
									SubscriptionId,
									resolvedEvent.OriginalEvent.EventStreamId,
									resolvedEvent.OriginalEvent.EventNumber,
									resolvedEvent.OriginalEvent.Position
								);

								await _eventAppeared(this, resolvedEvent, _cts.Token).ConfigureAwait(false);
								break;

							case StreamMessage.AllStreamCheckpointReached (var position):
								await _checkpointReached(this, position, _cts.Token)
									.ConfigureAwait(false);

								break;
						}
					} catch (Exception ex) when
						(ex is ObjectDisposedException or OperationCanceledException) {
						if (_subscriptionDroppedInvoked != 0) {
							return;
						}

						_log.LogWarning(
							ex,
							"Subscription {subscriptionId} was dropped because cancellation was requested by another caller.",
							SubscriptionId
						);

						SubscriptionDropped(SubscriptionDroppedReason.Disposed);

						return;
					} catch (Exception ex) {
						_log.LogError(
							ex,
							"Subscription {subscriptionId} was dropped because the subscriber made an error.",
							SubscriptionId
						);

						SubscriptionDropped(SubscriptionDroppedReason.SubscriberError, ex);

						return;
					}
				}
			} catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.Cancelled &&
			                                ex.Status.Detail.Contains("Call canceled by the client.")) {
				_log.LogInformation(
					"Subscription {subscriptionId} was dropped because cancellation was requested by the client.",
					SubscriptionId
				);

				SubscriptionDropped(SubscriptionDroppedReason.Disposed, ex);
			} catch (Exception ex) {
				if (_subscriptionDroppedInvoked == 0) {
					_log.LogError(
						ex,
						"Subscription {subscriptionId} was dropped because an error occurred on the server.",
						SubscriptionId
					);

					SubscriptionDropped(SubscriptionDroppedReason.ServerError, ex);
				}
			}
		}

		/// <inheritdoc />
		public void Dispose() => SubscriptionDropped(SubscriptionDroppedReason.Disposed);

		private void SubscriptionDropped(SubscriptionDroppedReason reason, Exception? ex = null) {
			if (Interlocked.CompareExchange(ref _subscriptionDroppedInvoked, 1, 0) == 1) {
				return;
			}

			try {
				_subscriptionDropped?.Invoke(this, reason, ex);
			} finally {
				_subscription.Dispose();
				_cts.Dispose();
			}
		}
	}
}
