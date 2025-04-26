using System.Threading.Channels;
using KurrentDB.Client.Diagnostics;
using EventStore.Client.Streams;
using Grpc.Core;
using KurrentDB.Client.Core.Serialization;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToAllAsync(
			SubscriptionListener listener,
			SubscribeToAllOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options                    ??= new SubscribeToAllOptions();
			listener.CheckpointReached ??= options.FilterOptions?.CheckpointReached;

			return StreamSubscription.Confirm(
				SubscribeToAll(options, cancellationToken),
				listener,
				_log,
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToAll(
			SubscribeToAllOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options ??= new SubscribeToAllOptions();

			return new StreamSubscriptionResult(
				async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
				new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks  = options.ResolveLinkTos ?? false,
						All           = ReadReq.Types.Options.Types.AllOptions.FromSubscriptionPosition(options.Start ?? FromAll.Start),
						Subscription  = new ReadReq.Types.Options.Types.SubscriptionOptions(),
						Filter        = GetFilterOptions(options.FilterOptions)!,
						UuidOption    = new() { Structured = new() }
					}
				},
				Settings,
				options.UserCredentials,
				_messageSerializer.With(options.SerializationSettings),
				cancellationToken
			);
		}

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="listener">Listener configured to receive notifications about new events and subscription state change.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromStream"/> from which to read, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<StreamSubscription> SubscribeToStreamAsync(
			string streamName,
			SubscriptionListener listener,
			SubscribeToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) =>
			StreamSubscription.Confirm(
				SubscribeToStream(streamName, options, cancellationToken),
				listener,
				_log,
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromStream"/> from which to read, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public StreamSubscriptionResult SubscribeToStream(
			string streamName,
			SubscribeToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options ??= new SubscribeToStreamOptions();

			return new StreamSubscriptionResult(
				async _ => await GetChannelInfo(cancellationToken).ConfigureAwait(false),
				new ReadReq {
					Options = new ReadReq.Types.Options {
						ReadDirection = ReadReq.Types.Options.Types.ReadDirection.Forwards,
						ResolveLinks  = options.ResolveLinkTos ?? false,
						Stream = ReadReq.Types.Options.Types.StreamOptions.FromSubscriptionPosition(
							streamName,
							options.Start ?? FromStream.Start
						),
						Subscription = new ReadReq.Types.Options.Types.SubscriptionOptions(),
						UuidOption   = new() { Structured = new() }
					}
				},
				Settings,
				options.UserCredentials,
				_messageSerializer.With(options.SerializationSettings),
				cancellationToken
			);
		}

		/// <summary>
		/// A class that represents the result of a subscription operation. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
		/// </summary>
		public class StreamSubscriptionResult : IAsyncEnumerable<ResolvedEvent>, IAsyncDisposable, IDisposable {
			readonly ReadReq                    _request;
			readonly Channel<StreamMessage>     _channel;
			readonly CancellationTokenSource    _cts;
			readonly CallOptions                _callOptions;
			readonly KurrentDBClientSettings    _settings;
			AsyncServerStreamingCall<ReadResp>? _call;

			int _messagesEnumerated;

			/// <summary>
			/// The server-generated unique identifier for the subscription.
			/// </summary>
			public string? SubscriptionId { get; private set; }

			/// <summary>
			/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<StreamMessage> Messages {
				get {
					if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1)
						throw new InvalidOperationException("Messages may only be enumerated once.");

					return GetMessages();

					async IAsyncEnumerable<StreamMessage> GetMessages() {
						try {
							await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)) {
								if (message is StreamMessage.SubscriptionConfirmation(var subscriptionId))
									SubscriptionId = subscriptionId;

								yield return message;
							}
						} finally {
#if NET8_0_OR_GREATER
							await _cts.CancelAsync().ConfigureAwait(false);
#else
							_cts.Cancel();
#endif
						}
					}
				}
			}

			internal StreamSubscriptionResult(
				Func<CancellationToken, Task<ChannelInfo>> selectChannelInfo,
				ReadReq request,
				KurrentDBClientSettings settings,
				UserCredentials? userCredentials,
				IMessageSerializer messageSerializer,
				CancellationToken cancellationToken
			) {
				_request  = request;
				_settings = settings;

				_callOptions = KurrentDBCallOptions.CreateStreaming(
					settings,
					userCredentials: userCredentials,
					cancellationToken: cancellationToken
				);

				_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

				if (_request.Options.FilterOptionCase == ReadReq.Types.Options.FilterOptionOneofCase.None) {
					_request.Options.NoFilter = new();
				}

				_ = PumpMessages();

				return;

				async Task PumpMessages() {
					try {
						var channelInfo = await selectChannelInfo(_cts.Token).ConfigureAwait(false);
						var client      = new EventStore.Client.Streams.Streams.StreamsClient(channelInfo.CallInvoker);
						_call = client.Read(_request, _callOptions);
						await foreach (var response in _call.ResponseStream.ReadAllAsync(_cts.Token)
							               .ConfigureAwait(false)) {
							StreamMessage subscriptionMessage =
								response.ContentCase switch {
									Confirmation => new StreamMessage.SubscriptionConfirmation(
										response.Confirmation.SubscriptionId
									),
									Event => new StreamMessage.Event(
										ConvertToResolvedEvent(response.Event, messageSerializer)
									),
									FirstStreamPosition => new StreamMessage.FirstStreamPosition(
										new StreamPosition(response.FirstStreamPosition)
									),
									LastStreamPosition => new StreamMessage.LastStreamPosition(
										new StreamPosition(response.LastStreamPosition)
									),
									LastAllStreamPosition => new StreamMessage.LastAllStreamPosition(
										new Position(
											response.LastAllStreamPosition.CommitPosition,
											response.LastAllStreamPosition.PreparePosition
										)
									),
									Checkpoint => new StreamMessage.AllStreamCheckpointReached(
										new Position(
											response.Checkpoint.CommitPosition,
											response.Checkpoint.PreparePosition
										)
									),
									CaughtUp => response.CaughtUp.Timestamp == null
										? StreamMessage.CaughtUp.Empty
										: new StreamMessage.CaughtUp(
											response.CaughtUp.Timestamp.ToDateTime(),
											response.CaughtUp.StreamRevision,
											new Position(response.CaughtUp.Position.CommitPosition, response.CaughtUp.Position.PreparePosition)),
									FellBehind => response.FellBehind.Timestamp == null
										? StreamMessage.FellBehind.Empty
										: new StreamMessage.FellBehind(
											response.FellBehind.Timestamp.ToDateTime(),
											response.FellBehind.StreamRevision,
											new Position(response.FellBehind.Position.CommitPosition, response.FellBehind.Position.PreparePosition)),
									_          => StreamMessage.Unknown.Instance
								};

							if (subscriptionMessage is StreamMessage.Event evt)
								KurrentDBClientDiagnostics.ActivitySource.TraceSubscriptionEvent(
									SubscriptionId,
									evt.ResolvedEvent,
									channelInfo,
									_settings,
									userCredentials
								);

							await _channel.Writer
								.WriteAsync(subscriptionMessage, _cts.Token)
								.ConfigureAwait(false);
						}

						_channel.Writer.Complete();
					} catch (Exception ex) {
						_channel.Writer.TryComplete(ex);
					}
				}
			}

			/// <inheritdoc />
			public async ValueTask DisposeAsync() {
				//TODO SS: Check if `CastAndDispose` is still relevant
				await CastAndDispose(_cts).ConfigureAwait(false);
				await CastAndDispose(_call).ConfigureAwait(false);

				return;

				static async ValueTask CastAndDispose(IDisposable? resource) {
					switch (resource) {
						case null:
							return;

						case IAsyncDisposable disposable:
							await disposable.DisposeAsync().ConfigureAwait(false);
							break;

						default:
							resource.Dispose();
							break;
					}
				}
			}

			/// <inheritdoc />
			public void Dispose() {
				_cts.Dispose();
				_call?.Dispose();
			}

			/// <inheritdoc />
			public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
				CancellationToken cancellationToken = default
			) {
				try {
					await foreach (var message in
					               _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
						if (message is not StreamMessage.Event e)
							continue;

						yield return e.ResolvedEvent;
					}
				} finally {
#if NET8_0_OR_GREATER
					await _cts.CancelAsync().ConfigureAwait(false);
#else
					_cts.Cancel();
#endif
				}
			}
		}
	}

	/// <summary>
	/// Subscribes to all events options.
	/// </summary>
	public class SubscribeToAllOptions {
		/// <summary>
		/// A <see cref="FromAll"/> (exclusive of) to start the subscription from.
		/// </summary>
		public FromAll? Start { get; set; }

		/// <summary>
		/// Whether to resolve LinkTo events automatically.
		/// </summary>
		public bool? ResolveLinkTos { get; set; }

		/// <summary>
		/// The optional <see cref="SubscriptionFilterOptions"/> to apply.
		/// </summary>
		public SubscriptionFilterOptions? FilterOptions { get; set; }

		/// <summary>
		/// The optional <see cref="SubscriptionFilterOptions"/> to apply.
		/// </summary>
		public IEventFilter Filter { set => FilterOptions = new SubscriptionFilterOptions(value); }

		/// <summary>
		/// The optional user credentials to perform operation with.
		/// </summary>
		public UserCredentials? UserCredentials { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }

		public static SubscribeToAllOptions Get() =>
			new SubscribeToAllOptions();

		public SubscribeToAllOptions WithFilter(SubscriptionFilterOptions filter) {
			FilterOptions = filter;

			return this;
		}
		
		public SubscribeToAllOptions WithFilter(IEventFilter filter) {
			Filter = filter;

			return this;
		}

		public SubscribeToAllOptions From(FromAll position) {
			Start = position;

			return this;
		}

		public SubscribeToAllOptions FromStart() {
			Start = FromAll.Start;

			return this;
		}

		public SubscribeToAllOptions FromEnd() {
			Start = FromAll.End;

			return this;
		}

		public SubscribeToAllOptions WithResolveLinkTos(bool resolve = true) {
			ResolveLinkTos = resolve;

			return this;
		}

		public SubscribeToAllOptions DisableAutoSerialization() {
			SerializationSettings = OperationSerializationSettings.Disabled;

			return this;
		}
	}

	/// <summary>
	/// Subscribes to all events options.
	/// </summary>
	public class SubscribeToStreamOptions {
		/// <summary>
		/// A <see cref="FromAll"/> (exclusive of) to start the subscription from.
		/// </summary>
		public FromStream? Start { get; set; }

		/// <summary>
		/// Whether to resolve LinkTo events automatically.
		/// </summary>
		public bool? ResolveLinkTos { get; set; }

		/// <summary>
		/// The optional user credentials to perform operation with.
		/// </summary>
		public UserCredentials? UserCredentials { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }

		public static SubscribeToStreamOptions Get() =>
			new SubscribeToStreamOptions();

		public SubscribeToStreamOptions From(FromStream position) {
			Start = position;

			return this;
		}

		public SubscribeToStreamOptions FromStart() {
			Start = FromStream.Start;

			return this;
		}

		public SubscribeToStreamOptions FromEnd() {
			Start = FromStream.End;

			return this;
		}

		public SubscribeToStreamOptions WithResolveLinkTos(bool resolve = true) {
			ResolveLinkTos = resolve;

			return this;
		}

		public SubscribeToStreamOptions DisableAutoSerialization() {
			SerializationSettings = OperationSerializationSettings.Disabled;

			return this;
		}
	}

	public class SubscriptionListener {
#if NET48
		/// <summary>
		/// A handler called when a new event is received over the subscription.
		/// </summary>
		public Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> EventAppeared { get; set; } = null!;
#else
		public required Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> EventAppeared { get; set; }
#endif
		/// <summary>
		/// A handler called if the subscription is dropped.
		/// </summary>
		public Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? SubscriptionDropped { get; set; }

		/// <summary>
		/// A handler called when a checkpoint is reached.
		/// Set the checkpointInterval in subscription filter options to define how often this method is called.
		/// </summary>
		public Func<StreamSubscription, Position, CancellationToken, Task>? CheckpointReached { get; set; }

		/// <summary>
		/// Returns the subscription listener with configured handlers
		/// </summary>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="subscriptionDropped">A handler invoked if the subscription is dropped.</param>
		/// <param name="checkpointReached">A handler called when a checkpoint is reached.
		/// Set the checkpointInterval in subscription filter options to define how often this method is called.
		/// </param>
		/// <returns></returns>
		public static SubscriptionListener Handle(
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			Func<StreamSubscription, Position, CancellationToken, Task>? checkpointReached = null
		) =>
			new SubscriptionListener {
				EventAppeared       = eventAppeared,
				SubscriptionDropped = subscriptionDropped,
				CheckpointReached   = checkpointReached
			};
	}

	public static class KurrentDBClientSubscribeExtensions {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="kurrentDbClient"></param>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToAllAsync(
			this KurrentDBClient kurrentDbClient,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			SubscribeToAllOptions? options = null,
			CancellationToken cancellationToken = default
		) =>
			kurrentDbClient.SubscribeToAllAsync(
				SubscriptionListener.Handle(eventAppeared),
				options,
				cancellationToken
			);

		/// <summary>
		/// Subscribes to messages from a specific stream
		/// </summary>
		/// <param name="kurrentDbClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="eventAppeared">Handler invoked when a new event is received over the subscription.</param>
		/// <param name="options">Optional settings like: Position <see cref="FromAll"/> from which to read, <see cref="SubscriptionFilterOptions"/> to apply, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<StreamSubscription> SubscribeToStreamAsync(
			this KurrentDBClient kurrentDbClient,
			string streamName,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			SubscribeToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) =>
			kurrentDbClient.SubscribeToStreamAsync(
				streamName,
				SubscriptionListener.Handle(eventAppeared),
				options,
				cancellationToken
			);
	}

	[Obsolete("Those extensions may be removed in the future versions", false)]
	public static class KurrentDBClientObsoleteSubscribeExtensions {
		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with SubscribeToAllOptions and get auto-serialization capabilities",
			false
		)]
		public static Task<StreamSubscription> SubscribeToAllAsync(
			this KurrentDBClient dbClient,
			FromAll start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = null,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) {
			var listener = SubscriptionListener.Handle(
				eventAppeared,
				subscriptionDropped,
				filterOptions?.CheckpointReached
			);

			var options = new SubscribeToAllOptions {
				Start                 = start,
				FilterOptions         = filterOptions,
				ResolveLinkTos        = resolveLinkTos,
				UserCredentials       = userCredentials,
				SerializationSettings = OperationSerializationSettings.Disabled,
			};

			return dbClient.SubscribeToAllAsync(listener, options, cancellationToken);
		}

		/// <summary>
		/// Subscribes to all events.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="start">A <see cref="FromAll"/> (exclusive of) to start the subscription from.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="filterOptions">The optional <see cref="SubscriptionFilterOptions"/> to apply.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with SubscribeToAllOptions and get auto-serialization capabilities",
			false
		)]
		public static KurrentDBClient.StreamSubscriptionResult SubscribeToAll(
			this KurrentDBClient dbClient,
			FromAll start,
			bool resolveLinkTos = false,
			SubscriptionFilterOptions? filterOptions = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.SubscribeToAll(
				new SubscribeToAllOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					FilterOptions         = filterOptions,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with SubscribeToStreamOptions and get auto-serialization capabilities",
			false
		)]
		public static KurrentDBClient.StreamSubscriptionResult SubscribeToStream(
			this KurrentDBClient dbClient,
			string streamName,
			FromStream start,
			bool resolveLinkTos = false,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.SubscribeToStream(
				streamName,
				new SubscribeToStreamOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Subscribes to a stream from a <see cref="StreamPosition">checkpoint</see>.
		/// </summary>
		/// <param name="start">A <see cref="FromStream"/> (exclusive of) to start the subscription from.</param>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to subscribe for notifications about new events.</param>
		/// <param name="eventAppeared">A Task invoked and awaited when a new event is received over the subscription.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="subscriptionDropped">An action invoked if the subscription is dropped.</param>
		/// <param name="userCredentials">The optional user credentials to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with SubscribeToStreamOptions and get auto-serialization capabilities",
			false
		)]
		public static Task<StreamSubscription> SubscribeToStreamAsync(
			this KurrentDBClient dbClient,
			string streamName,
			FromStream start,
			Func<StreamSubscription, ResolvedEvent, CancellationToken, Task> eventAppeared,
			bool resolveLinkTos = false,
			Action<StreamSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped = default,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.SubscribeToStreamAsync(
				streamName,
				SubscriptionListener.Handle(eventAppeared, subscriptionDropped),
				new SubscribeToStreamOptions {
					Start                 = start,
					ResolveLinkTos        = resolveLinkTos,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);
	}
}
