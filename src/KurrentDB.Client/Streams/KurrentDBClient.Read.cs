using System.Runtime.CompilerServices;
using System.Threading.Channels;
using EventStore.Client.Streams;
using Grpc.Core;
using KurrentDB.Client.Core.Serialization;
using static EventStore.Client.Streams.ReadResp;
using static EventStore.Client.Streams.ReadResp.ContentOneofCase;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Asynchronously reads all events. By default, it reads all of them from the start. The options parameter allows you to fine-tune it to your needs.
		/// </summary>
		/// <param name="options">Optional settings like: max count, <see cref="Direction"/> in which to read, the <see cref="Position"/> to start reading from, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public ReadAllStreamResult ReadAllAsync(
			ReadAllOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options ??= new ReadAllOptions();

			if (options.MaxCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(options.MaxCount));

			var readReq = new ReadReq {
				Options = new() {
					ReadDirection = options.Direction switch {
						Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
						Direction.Forwards  => ReadReq.Types.Options.Types.ReadDirection.Forwards,
						null                => ReadReq.Types.Options.Types.ReadDirection.Forwards,
						_                   => throw InvalidOption(options.Direction.Value)
					},
					ResolveLinks = options.ResolveLinkTos ?? false,
					All = new() {
						Position = new() {
							CommitPosition  = (options.Position ?? Position.Start).CommitPosition,
							PreparePosition = (options.Position ?? Position.Start).PreparePosition
						}
					},
					Count         = (ulong)(options.MaxCount ?? long.MaxValue),
					UuidOption    = new() { Structured    = new() },
					ControlOption = new() { Compatibility = 1 },
					Filter        = GetFilterOptions(options.Filter)
				}
			};

			return new ReadAllStreamResult(
				async _ => {
					var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
					return channelInfo.CallInvoker;
				},
				readReq,
				Settings,
				options,
				_messageSerializer.With(options.SerializationSettings),
				cancellationToken
			);
		}

		/// <summary>
		/// A class that represents the result of a read operation on the $all stream. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
		/// </summary>
		public class ReadAllStreamResult : IAsyncEnumerable<ResolvedEvent> {
			readonly Channel<StreamMessage>  _channel;
			readonly CancellationTokenSource _cts;

			int _messagesEnumerated;

			/// <summary>
			/// The last <see cref="Position"/> of the $all stream, if available.
			/// </summary>
			public Position? LastPosition { get; private set; }

			/// <summary>
			/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<StreamMessage> Messages {
				get {
					return GetMessages();

					async IAsyncEnumerable<StreamMessage> GetMessages() {
						if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1) {
							throw new InvalidOperationException("Messages may only be enumerated once.");
						}

						try {
							await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)
								               .ConfigureAwait(false)) {
								if (message is StreamMessage.LastAllStreamPosition(var position)) {
									LastPosition = position;
								}

								yield return message;
							}
						} finally {
							_cts.Cancel();
						}
					}
				}
			}

			internal ReadAllStreamResult(
				Func<CancellationToken, Task<CallInvoker>> selectCallInvoker,
				ReadReq request,
				KurrentDBClientSettings settings,
				ReadAllOptions options,
				IMessageSerializer messageSerializer,
				CancellationToken cancellationToken
			) {
				var callOptions = KurrentDBCallOptions.CreateStreaming(
					settings,
					options.Deadline,
					options.UserCredentials,
					cancellationToken
				);

				_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				var linkedCancellationToken = _cts.Token;

				if (request.Options.FilterOptionCase == ReadReq.Types.Options.FilterOptionOneofCase.None)
					request.Options.NoFilter = new();

				_ = PumpMessages();

				return;

				async Task PumpMessages() {
					try {
						var       callInvoker = await selectCallInvoker(linkedCancellationToken).ConfigureAwait(false);
						var       client      = new EventStore.Client.Streams.Streams.StreamsClient(callInvoker);
						using var call        = client.Read(request, callOptions);

						await foreach (var response in call.ResponseStream.ReadAllAsync(linkedCancellationToken)
							               .ConfigureAwait(false)) {
							await _channel.Writer.WriteAsync(
								response.ContentCase switch {
									StreamNotFound => StreamMessage.NotFound.Instance,
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
									_ => StreamMessage.Unknown.Instance
								},
								linkedCancellationToken
							).ConfigureAwait(false);
						}

						_channel.Writer.Complete();
					} catch (Exception ex) {
						_channel.Writer.TryComplete(ex);
					}
				}
			}

			/// <inheritdoc />
			public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
				CancellationToken cancellationToken = default
			) {
				try {
					await foreach (var message in
					               _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
						if (message is not StreamMessage.Event e) {
							continue;
						}

						yield return e.ResolvedEvent;
					}
				} finally {
					_cts.Cancel();
				}
			}
		}

		/// <summary>
		/// Asynchronously reads all the events from a stream.
		/// 
		/// The result could also be inspected as a means to avoid handling exceptions as the <see cref="ReadState"/> would indicate whether or not the stream is readable./>
		/// </summary>
		/// <param name="streamName">The name of the stream to read.</param>
		/// <param name="options">Optional settings like: max count, <see cref="Direction"/> in which to read, the <see cref="Position"/> to start reading from, etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public ReadStreamResult ReadStreamAsync(
			string streamName,
			ReadStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options ??= new ReadStreamOptions();

			if (options.MaxCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(options.MaxCount));

			return new ReadStreamResult(
				async _ => {
					var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
					return channelInfo.CallInvoker;
				},
				new ReadReq {
					Options = new() {
						ReadDirection = options.Direction switch {
							Direction.Backwards => ReadReq.Types.Options.Types.ReadDirection.Backwards,
							Direction.Forwards  => ReadReq.Types.Options.Types.ReadDirection.Forwards,
							null                => ReadReq.Types.Options.Types.ReadDirection.Forwards,
							_                   => throw InvalidOption(options.Direction.Value)
						},
						ResolveLinks = options.ResolveLinkTos ?? false,
						Stream = ReadReq.Types.Options.Types.StreamOptions.FromStreamNameAndRevision(
							streamName,
							options.StreamPosition ?? StreamPosition.Start
						),
						Count         = (ulong)(options.MaxCount ?? long.MaxValue),
						UuidOption    = new() { Structured = new() },
						NoFilter      = new(),
						ControlOption = new() { Compatibility = 1 }
					}
				},
				Settings,
				options.Deadline,
				options.UserCredentials,
				_messageSerializer.With(options.SerializationSettings),
				cancellationToken
			);
		}

		/// <summary>
		/// A class that represents the result of a read operation on a stream. You may either enumerate this instance directly or <see cref="Messages"/>. Do not enumerate more than once.
		/// </summary>
		public class ReadStreamResult : IAsyncEnumerable<ResolvedEvent> {
			readonly Channel<StreamMessage>  _channel;
			readonly CancellationTokenSource _cts;

			int _messagesEnumerated;

			/// <summary>
			/// The name of the stream.
			/// </summary>
			public string StreamName { get; }

			/// <summary>
			/// The <see cref="StreamPosition"/> of the first message in this stream. Will only be filled once <see cref="Messages"/> has been enumerated. 
			/// </summary>
			public StreamPosition? FirstStreamPosition { get; private set; }

			/// <summary>
			/// The <see cref="StreamPosition"/> of the last message in this stream. Will only be filled once <see cref="Messages"/> has been enumerated. 
			/// </summary>
			public StreamPosition? LastStreamPosition { get; private set; }

			/// <summary>
			/// An <see cref="IAsyncEnumerable{StreamMessage}"/>. Do not enumerate more than once.
			/// </summary>
			public IAsyncEnumerable<StreamMessage> Messages {
				get {
					return GetMessages();

					async IAsyncEnumerable<StreamMessage> GetMessages() {
						if (Interlocked.Exchange(ref _messagesEnumerated, 1) == 1) {
							throw new InvalidOperationException("Messages may only be enumerated once.");
						}

						try {
							await foreach (var message in _channel.Reader.ReadAllAsync(_cts.Token)
								               .ConfigureAwait(false)) {
								switch (message) {
									case StreamMessage.FirstStreamPosition(var streamPosition):
										FirstStreamPosition = streamPosition;
										break;

									case StreamMessage.LastStreamPosition(var lastStreamPosition):
										LastStreamPosition = lastStreamPosition;
										break;

									default:
										break;
								}

								yield return message;
							}
						} finally {
							_cts.Cancel();
						}
					}
				}
			}

			/// <summary>
			/// The <see cref="ReadState"/>.
			/// </summary>
			public Task<ReadState> ReadState { get; }

			internal ReadStreamResult(
				Func<CancellationToken, Task<CallInvoker>> selectCallInvoker,
				ReadReq request,
				KurrentDBClientSettings settings,
				TimeSpan? deadline,
				UserCredentials? userCredentials,
				IMessageSerializer messageSerializer,
				CancellationToken cancellationToken
			) {
				var callOptions = KurrentDBCallOptions.CreateStreaming(
					settings,
					deadline,
					userCredentials,
					cancellationToken
				);

				_channel = Channel.CreateBounded<StreamMessage>(ReadBoundedChannelOptions);

				StreamName = request.Options.Stream.StreamIdentifier!;

				var tcs = new TaskCompletionSource<ReadState>();
				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				var linkedCancellationToken = _cts.Token;
#pragma warning disable CS0612
				ReadState = tcs.Task;
#pragma warning restore CS0612

				_ = PumpMessages();

				return;

				async Task PumpMessages() {
					var firstMessageRead = false;

					try {
						var       callInvoker = await selectCallInvoker(linkedCancellationToken).ConfigureAwait(false);
						var       client      = new EventStore.Client.Streams.Streams.StreamsClient(callInvoker);
						using var call        = client.Read(request, callOptions);

						await foreach (var response in call.ResponseStream.ReadAllAsync(linkedCancellationToken)
							               .ConfigureAwait(false)) {
							if (!firstMessageRead) {
								firstMessageRead = true;

								if (response.ContentCase != StreamNotFound || request.Options.Stream == null) {
									await _channel.Writer.WriteAsync(StreamMessage.Ok.Instance, linkedCancellationToken)
										.ConfigureAwait(false);

									tcs.SetResult(Client.ReadState.Ok);
								} else {
									tcs.SetResult(Client.ReadState.StreamNotFound);
								}
							}

							await _channel.Writer.WriteAsync(
								response.ContentCase switch {
									StreamNotFound => StreamMessage.NotFound.Instance,
									Event => new StreamMessage.Event(
										ConvertToResolvedEvent(response.Event, messageSerializer)
									),
									ContentOneofCase.FirstStreamPosition => new StreamMessage.FirstStreamPosition(
										new StreamPosition(response.FirstStreamPosition)
									),
									ContentOneofCase.LastStreamPosition => new StreamMessage.LastStreamPosition(
										new StreamPosition(response.LastStreamPosition)
									),
									LastAllStreamPosition => new StreamMessage.LastAllStreamPosition(
										new Position(
											response.LastAllStreamPosition.CommitPosition,
											response.LastAllStreamPosition.PreparePosition
										)
									),
									_ => StreamMessage.Unknown.Instance
								},
								linkedCancellationToken
							).ConfigureAwait(false);
						}

						_channel.Writer.Complete();
					} catch (Exception ex) {
						tcs.TrySetException(ex);
						_channel.Writer.TryComplete(ex);
					}
				}
			}

			/// <inheritdoc />
			public async IAsyncEnumerator<ResolvedEvent> GetAsyncEnumerator(
				CancellationToken cancellationToken = default
			) {
				try {
					await foreach (var message in
					               _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
						if (message is StreamMessage.NotFound) {
							throw new StreamNotFoundException(StreamName);
						}

						if (message is not StreamMessage.Event e) {
							continue;
						}

						yield return e.ResolvedEvent;
					}
				} finally {
					_cts.Cancel();
				}
			}
		}

		static ResolvedEvent ConvertToResolvedEvent(
			Types.ReadEvent readEvent,
			IMessageSerializer messageSerializer
		) =>
			ResolvedEvent.From(
				ConvertToEventRecord(readEvent.Event)!,
				ConvertToEventRecord(readEvent.Link),
				readEvent.PositionCase switch {
					Types.ReadEvent.PositionOneofCase.CommitPosition => readEvent.CommitPosition,
					_                                                => null
				},
				messageSerializer
			);

		static EventRecord? ConvertToEventRecord(ReadResp.Types.ReadEvent.Types.RecordedEvent? e) =>
			e == null
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

	/// <summary>
	/// Optional settings to customize reading all messages, for instance: max count,
	/// <see cref="Direction"/> in which to read, the <see cref="Position"/> to start reading from, etc.
	/// </summary>
	public class ReadAllOptions : OperationOptions {
		/// <summary>
		/// The <see cref="Direction"/> in which to read. When not provided Forwards is used.
		/// </summary>
		public Direction? Direction { get; set; }

		/// <summary>
		/// The <see cref="Position"/> to start reading from. When not provided Start is used.
		/// </summary>
		public Position? Position { get; set; }

		/// <summary>
		/// The <see cref="IEventFilter"/> to apply.
		/// </summary>
		public IEventFilter? Filter { get; set; }

		/// <summary>
		/// The number of events to read from the stream. When not provided, no limit is set.
		/// </summary>
		public long? MaxCount { get; set; }

		/// <summary>
		/// Whether to resolve LinkTo events automatically. When not provided, false is used.
		/// </summary>
		public bool? ResolveLinkTos { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }

		public static ReadAllOptions Get() =>
			new ReadAllOptions();

		public ReadAllOptions WithFilter(IEventFilter filter) {
			Filter = filter;

			return this;
		}

		public ReadAllOptions Forwards() {
			Direction =   KurrentDB.Client.Direction.Forwards;
			Position  ??= KurrentDB.Client.Position.Start;

			return this;
		}

		public ReadAllOptions Backwards() {
			Direction =   KurrentDB.Client.Direction.Backwards;
			Position  ??= KurrentDB.Client.Position.End;

			return this;
		}

		public ReadAllOptions From(Position position) {
			this.Position = position;

			return this;
		}

		public ReadAllOptions FromStart() {
			Position  =   KurrentDB.Client.Position.Start;
			Direction ??= Client.Direction.Forwards;

			return this;
		}

		public ReadAllOptions FromEnd() {
			Position  =   KurrentDB.Client.Position.End;
			Direction ??= Client.Direction.Backwards;

			return this;
		}

		public ReadAllOptions WithResolveLinkTos(bool resolve = true) {
			ResolveLinkTos = resolve;

			return this;
		}

		public ReadAllOptions Max(long maxCount) {
			MaxCount = maxCount;

			return this;
		}

		public ReadAllOptions MaxOne() =>
			Max(1);

		public ReadAllOptions First() =>
			FromStart()
				.Forwards()
				.MaxOne();

		public ReadAllOptions Last() =>
			FromEnd()
				.Backwards()
				.MaxOne();

		public ReadAllOptions DisableAutoSerialization() {
			SerializationSettings = OperationSerializationSettings.Disabled;

			return this;
		}
	}

	/// <summary>
	/// Optional settings to customize reading stream messages, for instance: max count,
	/// <see cref="Direction"/> in which to read, the <see cref="StreamPosition"/> to start reading from, etc.
	/// </summary>
	public class ReadStreamOptions : OperationOptions {
		/// <summary>
		/// The <see cref="Direction"/> in which to read.
		/// </summary>
		public Direction? Direction { get; set; }

		/// <summary>
		/// The <see cref="Client.StreamRevision"/> to start reading from.
		/// </summary>
		public StreamPosition? StreamPosition { get; set; }

		/// <summary>
		/// The number of events to read from the stream.
		/// </summary>
		public long? MaxCount { get; set; }

		/// <summary>
		/// Whether to resolve LinkTo events automatically.
		/// </summary>
		public bool? ResolveLinkTos { get; set; }

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }

		public static ReadStreamOptions Get() =>
			new ReadStreamOptions();

		public ReadStreamOptions Forwards() {
			Direction      =   KurrentDB.Client.Direction.Forwards;
			StreamPosition ??= KurrentDB.Client.StreamPosition.Start;

			return this;
		}

		public ReadStreamOptions Backwards() {
			Direction      =   KurrentDB.Client.Direction.Backwards;
			StreamPosition ??= KurrentDB.Client.StreamPosition.End;

			return this;
		}

		public ReadStreamOptions From(StreamPosition streamPosition) {
			StreamPosition = streamPosition;

			return this;
		}

		public ReadStreamOptions FromStart() {
			StreamPosition =   KurrentDB.Client.StreamPosition.Start;
			Direction      ??= Client.Direction.Forwards;

			return this;
		}

		public ReadStreamOptions FromEnd() {
			StreamPosition =   KurrentDB.Client.StreamPosition.End;
			Direction      ??= Client.Direction.Backwards;

			return this;
		}

		public ReadStreamOptions WithResolveLinkTos(bool resolve = true) {
			ResolveLinkTos = resolve;

			return this;
		}

		public ReadStreamOptions Max(long maxCount) {
			MaxCount = maxCount;

			return this;
		}

		public ReadStreamOptions MaxOne() =>
			Max(1);

		public ReadStreamOptions First() =>
			FromStart()
				.Forwards()
				.MaxOne();

		public ReadStreamOptions Last() =>
			FromEnd()
				.Backwards()
				.MaxOne();

		public ReadStreamOptions DisableAutoSerialization() {
			SerializationSettings = OperationSerializationSettings.Disabled;

			return this;
		}
	}

	[Obsolete("Those extensions may be removed in the future versions", false)]
	public static class ObsoleteKurrentDBClientReadExtensions {
		/// <summary>
		/// Asynchronously reads all events.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
		/// <param name="position">The <see cref="Position"/> to start reading from.</param>
		/// <param name="maxCount">The maximum count to read.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with ReadAllOptions and get auto-serialization capabilities",
			false
		)]
		public static KurrentDBClient.ReadAllStreamResult ReadAllAsync(
			this KurrentDBClient dbClient,
			Direction direction,
			Position position,
			long maxCount = long.MaxValue,
			bool resolveLinkTos = false,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.ReadAllAsync(
				new ReadAllOptions {
					Direction             = direction,
					Position              = position,
					Filter                = null,
					MaxCount              = maxCount,
					ResolveLinkTos        = resolveLinkTos,
					Deadline              = deadline,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);

		/// <summary>
		/// Asynchronously reads all events with filtering.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
		/// <param name="position">The <see cref="Position"/> to start reading from.</param>
		/// <param name="eventFilter">The <see cref="IEventFilter"/> to apply.</param>
		/// <param name="maxCount">The maximum count to read.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with ReadAllOptions and get auto-serialization capabilities",
			false
		)]
		public static KurrentDBClient.ReadAllStreamResult ReadAllAsync(
			this KurrentDBClient dbClient,
			Direction direction,
			Position position,
			IEventFilter? eventFilter,
			long maxCount = long.MaxValue,
			bool resolveLinkTos = false,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) {
			if (maxCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount));

			return dbClient.ReadAllAsync(
				new ReadAllOptions {
					Direction             = direction,
					Position              = position,
					Filter                = eventFilter,
					MaxCount              = maxCount,
					ResolveLinkTos        = resolveLinkTos,
					Deadline              = deadline,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);
		}

		/// <summary>
		/// Asynchronously reads all the events from a stream.
		/// 
		/// The result could also be inspected as a means to avoid handling exceptions as the <see cref="ReadState"/> would indicate whether or not the stream is readable./>
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="direction">The <see cref="Direction"/> in which to read.</param>
		/// <param name="streamName">The name of the stream to read.</param>
		/// <param name="revision">The <see cref="StreamRevision"/> to start reading from.</param>
		/// <param name="maxCount">The number of events to read from the stream.</param>
		/// <param name="resolveLinkTos">Whether to resolve LinkTo events automatically.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with ReadStreamOptions and get auto-serialization capabilities",
			false
		)]
		public static KurrentDBClient.ReadStreamResult ReadStreamAsync(
			this KurrentDBClient dbClient,
			Direction direction,
			string streamName,
			StreamPosition revision,
			long maxCount = long.MaxValue,
			bool resolveLinkTos = false,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) {
			if (maxCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount));

			return dbClient.ReadStreamAsync(
				streamName,
				new ReadStreamOptions {
					Direction             = direction,
					StreamPosition        = revision,
					MaxCount              = maxCount,
					ResolveLinkTos        = resolveLinkTos,
					Deadline              = deadline,
					UserCredentials       = userCredentials,
					SerializationSettings = OperationSerializationSettings.Disabled
				},
				cancellationToken
			);
		}
	}

	public static class ReadMessagesExtensions {
		public static async IAsyncEnumerable<object> DeserializedData(
			this IAsyncEnumerable<ResolvedEvent> resolvedEvents,
			[EnumeratorCancellation] CancellationToken ct = default
		) {
			await foreach (var resolvedEvent in resolvedEvents.WithCancellation(ct)) {
				if (resolvedEvent.DeserializedData != null)
					yield return resolvedEvent.DeserializedData;
			}
		}

		public static async IAsyncEnumerable<Message> DeserializedMessages(
			this IAsyncEnumerable<ResolvedEvent> resolvedEvents,
			[EnumeratorCancellation] CancellationToken ct = default
		) {
			await foreach (var resolvedEvent in resolvedEvents.WithCancellation(ct)) {
				if (resolvedEvent.Message != null)
					yield return resolvedEvent.Message;
			}
		}
	}
}
