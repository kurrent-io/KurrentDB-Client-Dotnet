using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Google.Protobuf;
using EventStore.Client.Streams;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Client.Core.Serialization;
using Kurrent.Diagnostics;
using Kurrent.Diagnostics.Telemetry;
using Kurrent.Diagnostics.Tracing;
using static EventStore.Client.Streams.AppendResp.Types.WrongExpectedVersion;
using static EventStore.Client.Streams.Streams;
using static KurrentDB.Client.Core.Serialization.MessageTypeNamingResolutionContext;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Appends events asynchronously to a stream. Messages are serialized using default or custom serialization configured through <see cref="KurrentDBClientSettings"/>
		/// </summary>
		/// <param name="streamName">The name of the stream to append events to.</param>
		/// <param name="expectedState">The expected <see cref="KurrentDB.Client.StreamState"/> of the stream to append to.</param>
		/// <param name="messages">Messages to append to the stream.</param>
		/// <param name="options">Optional settings for the append operation, e.g. deadline, user credentials etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<Message> messages,
			AppendToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			var messageSerializationContext = new MessageSerializationContext(FromStreamName(streamName));

			var messageData = _messageSerializer.With(options?.SerializationSettings)
				.Serialize(messages, messageSerializationContext);

			return AppendToStreamAsync(streamName, expectedState, messageData, options, cancellationToken);
		}

		/// <summary>
		/// Appends events asynchronously to a stream using raw message data.
		/// If you want to use auto-serialization, use overload with <see cref="Message"/>.</param>.
		/// This method intends to cover low-level scenarios in which you want to have full control of the serialization mechanism.
		/// </summary>
		/// <param name="streamName">The name of the stream to append events to.</param>
		/// <param name="expectedState">The expected <see cref="KurrentDB.Client.StreamState"/> of the stream to append to.</param>
		/// <param name="messageData">Raw message data to append to the stream.</param>
		/// <param name="options">Optional settings for the append operation, e.g. deadline, user credentials etc.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public async Task<IWriteResult> AppendToStreamAsync(
			string streamName,
			StreamState expectedState,
			IEnumerable<MessageData> messageData,
			AppendToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			options ??= new AppendToStreamOptions();
			options.With(Settings.OperationOptions);

			_log.LogDebug("Append to stream - {streamName}@{expectedState}.", streamName, expectedState);

			var task =
				options.UserCredentials == null && await BatchAppender.IsUsable().ConfigureAwait(false)
					? BatchAppender.Append(streamName, expectedState, messageData, options.Deadline, cancellationToken)
					: AppendToStreamInternal(
						await GetChannelInfo(cancellationToken).ConfigureAwait(false),
						new AppendReq {
							Options = new() {
								StreamIdentifier = streamName
							}
						}.WithAnyStreamRevision(expectedState),
						messageData,
						options,
						cancellationToken
					);

			return (await task.ConfigureAwait(false)).OptionallyThrowWrongExpectedVersionException(options);
		}

		ValueTask<IWriteResult> AppendToStreamInternal(
			ChannelInfo channelInfo,
			AppendReq header,
			IEnumerable<MessageData> messageData,
			AppendToStreamOptions operationOptions,
			CancellationToken cancellationToken
		) {
			var userCredentials = operationOptions.UserCredentials;
			var deadline        = operationOptions.Deadline;

			var tags = new ActivityTagsCollection()
				.WithRequiredTag(
					TelemetryTags.Kurrent.Stream,
					header.Options.StreamIdentifier.StreamName.ToStringUtf8()
				)
				.WithGrpcChannelServerTags(channelInfo)
				.WithClientSettingsServerTags(Settings)
				.WithOptionalTag(
					TelemetryTags.Database.User,
					userCredentials?.Username ?? Settings.DefaultCredentials?.Username
				);

			return KurrentDBClientDiagnostics.ActivitySource.TraceClientOperation(
				Operation,
				TracingConstants.Operations.Append,
				tags
			);

			async ValueTask<IWriteResult> Operation() {
				using var call = new StreamsClient(channelInfo.CallInvoker)
					.Append(
						KurrentDBCallOptions.CreateNonStreaming(Settings, deadline, userCredentials, cancellationToken)
					);

				await call.RequestStream
					.WriteAsync(header)
					.ConfigureAwait(false);

				foreach (var e in messageData) {
					var appendReq = new AppendReq {
						ProposedMessage = new() {
							Id             = e.MessageId.ToDto(),
							Data           = ByteString.CopyFrom(e.Data.Span),
							CustomMetadata = ByteString.CopyFrom(e.Metadata.InjectTracingContext(Activity.Current)),
							Metadata = {
								{ Constants.Metadata.Type, e.Type },
								{ Constants.Metadata.ContentType, e.ContentType }
							}
						}
					};

					await call.RequestStream.WriteAsync(appendReq).ConfigureAwait(false);
				}

				await call.RequestStream.CompleteAsync().ConfigureAwait(false);

				var response = await call.ResponseAsync.ConfigureAwait(false);

				if (response.Success is not null)
					return HandleSuccessAppend(response, header);

				if (response.WrongExpectedVersion is null)
					throw new InvalidOperationException("The operation completed with an unexpected result.");

				return HandleWrongExpectedRevision(response, header, operationOptions);
			}
		}

		IWriteResult HandleSuccessAppend(AppendResp response, AppendReq header) {
			var currentRevision = response.Success.CurrentRevisionOptionCase
			                   == AppendResp.Types.Success.CurrentRevisionOptionOneofCase.NoStream
				? StreamState.NoStream
				: StreamState.StreamRevision(response.Success.CurrentRevision);

			var position = response.Success.PositionOptionCase
			            == AppendResp.Types.Success.PositionOptionOneofCase.Position
				? new Position(response.Success.Position.CommitPosition, response.Success.Position.PreparePosition)
				: default;

			_log.LogDebug(
				"Append to stream succeeded - {streamName}@{logPosition}/{nextExpectedVersion}.",
				header.Options.StreamIdentifier,
				position,
				currentRevision
			);

			return new SuccessResult(currentRevision, position);
		}

		IWriteResult HandleWrongExpectedRevision(
			AppendResp response,
			AppendReq header,
			AppendToStreamOptions operationOptions
		) {
			var actualStreamRevision = response.WrongExpectedVersion.CurrentRevisionOptionCase
			                        == CurrentRevisionOptionOneofCase.CurrentRevision
				? StreamState.StreamRevision(response.WrongExpectedVersion.CurrentRevision)
				: StreamState.NoStream;

			_log.LogDebug(
				"Append to stream failed with Wrong Expected Version - {streamName}/{expectedRevision}/{currentRevision}",
				header.Options.StreamIdentifier,
				StreamState.StreamRevision(header.Options.Revision),
				actualStreamRevision
			);

			if (operationOptions.ThrowOnAppendFailure == true) {
				if (response.WrongExpectedVersion.ExpectedRevisionOptionCase
				 == ExpectedRevisionOptionOneofCase.ExpectedRevision) {
					throw new WrongExpectedVersionException(
						header.Options.StreamIdentifier!,
						StreamState.StreamRevision(response.WrongExpectedVersion.ExpectedRevision),
						actualStreamRevision
					);
				}

				var expectedStreamState = response.WrongExpectedVersion.ExpectedRevisionOptionCase switch {
					ExpectedRevisionOptionOneofCase.ExpectedAny          => StreamState.Any,
					ExpectedRevisionOptionOneofCase.ExpectedNoStream     => StreamState.NoStream,
					ExpectedRevisionOptionOneofCase.ExpectedStreamExists => StreamState.StreamExists,
					_                                                    => StreamState.Any
				};

				throw new WrongExpectedVersionException(
					header.Options.StreamIdentifier!,
					expectedStreamState,
					actualStreamRevision
				);
			}

			var expectedRevision = response.WrongExpectedVersion.ExpectedRevisionOptionCase
			                    == ExpectedRevisionOptionOneofCase.ExpectedRevision
				? StreamState.StreamRevision(response.WrongExpectedVersion.ExpectedRevision)
				: StreamState.NoStream;

			return new WrongExpectedVersionResult(
				header.Options.StreamIdentifier!,
				expectedRevision,
				actualStreamRevision
			);
		}

		class StreamAppender : IDisposable {
			readonly KurrentDBClientSettings                                        _settings;
			readonly CancellationToken                                              _cancellationToken;
			readonly Action<Exception>                                              _onException;
			readonly Channel<BatchAppendReq>                                        _channel;
			readonly ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>> _pendingRequests;
			readonly TaskCompletionSource<bool>                                     _isUsable;

			ChannelInfo?                                               _channelInfo;
			AsyncDuplexStreamingCall<BatchAppendReq, BatchAppendResp>? _call;

			public StreamAppender(
				KurrentDBClientSettings settings,
				ValueTask<ChannelInfo> channelInfoTask,
				CancellationToken cancellationToken,
				Action<Exception> onException
			) {
				_settings          = settings;
				_cancellationToken = cancellationToken;
				_onException       = onException;
				_channel           = Channel.CreateBounded<BatchAppendReq>(10000);
				_pendingRequests   = new ConcurrentDictionary<Uuid, TaskCompletionSource<IWriteResult>>();
				_isUsable          = new TaskCompletionSource<bool>();

				_ = Task.Run(() => Duplex(channelInfoTask), cancellationToken);
			}

			public ValueTask<IWriteResult> Append(
				string streamName,
				StreamState expectedStreamState,
				IEnumerable<MessageData> events,
				TimeSpan? timeoutAfter,
				CancellationToken cancellationToken = default
			) =>
				AppendInternal(
					BatchAppendReq.Types.Options.Create(streamName, expectedStreamState, timeoutAfter),
					events,
					cancellationToken
				);

			public Task<bool> IsUsable() => _isUsable.Task;

			ValueTask<IWriteResult> AppendInternal(
				BatchAppendReq.Types.Options options,
				IEnumerable<MessageData> events,
				CancellationToken cancellationToken
			) {
				var tags = new ActivityTagsCollection()
					.WithRequiredTag(TelemetryTags.Kurrent.Stream, options.StreamIdentifier.StreamName.ToStringUtf8())
					.WithGrpcChannelServerTags(_channelInfo)
					.WithClientSettingsServerTags(_settings)
					.WithOptionalTag(TelemetryTags.Database.User, _settings.DefaultCredentials?.Username);

				return KurrentDBClientDiagnostics.ActivitySource.TraceClientOperation(
					Operation,
					TracingConstants.Operations.Append,
					tags
				);

				async ValueTask<IWriteResult> Operation() {
					var correlationId = Uuid.NewUuid();

					var complete = _pendingRequests.GetOrAdd(correlationId, new TaskCompletionSource<IWriteResult>());

					try {
						foreach (var appendRequest in GetRequests(events, options, correlationId))
							await _channel.Writer.WriteAsync(appendRequest, cancellationToken).ConfigureAwait(false);
					} catch (ChannelClosedException ex) {
						// channel is closed, our tcs won't necessarily get completed, don't wait for it.
						throw ex.InnerException ?? ex;
					}

					return await complete.Task.ConfigureAwait(false);
				}
			}

			async Task Duplex(ValueTask<ChannelInfo> channelInfoTask) {
				try {
					_channelInfo = await channelInfoTask.ConfigureAwait(false);
					if (!_channelInfo.ServerCapabilities.SupportsBatchAppend) {
						_channel.Writer.TryComplete(new NotSupportedException("Server does not support batch append"));
						_isUsable.TrySetResult(false);
						return;
					}

					_call = new StreamsClient(_channelInfo.CallInvoker).BatchAppend(
						KurrentDBCallOptions.CreateStreaming(
							_settings,
							userCredentials: _settings.DefaultCredentials,
							cancellationToken: _cancellationToken
						)
					);

					_ = Task.Run(Send, _cancellationToken);
					_ = Task.Run(Receive, _cancellationToken);

					_isUsable.TrySetResult(true);
				} catch (Exception ex) {
					_isUsable.TrySetException(ex);
					_onException(ex);
				}

				return;

				async Task Send() {
					if (_call is null) return;

					await foreach (var appendRequest in _channel.Reader.ReadAllAsync(_cancellationToken)
						               .ConfigureAwait(false))
						await _call.RequestStream.WriteAsync(appendRequest).ConfigureAwait(false);

					await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
				}

				async Task Receive() {
					if (_call is null) return;

					try {
						await foreach (var response in _call.ResponseStream.ReadAllAsync(_cancellationToken)
							               .ConfigureAwait(false)) {
							if (!_pendingRequests.TryRemove(
								    Uuid.FromDto(response.CorrelationId),
								    out var writeResult
							    )) {
								continue; // TODO: Log?
							}

							try {
								writeResult.TrySetResult(response.ToWriteResult());
							} catch (Exception ex) {
								writeResult.TrySetException(ex);
							}
						}
					} catch (Exception ex) {
						// signal that no tcs added to _pendingRequests after this point will necessarily complete
						_channel.Writer.TryComplete(ex);

						// complete whatever tcs's we have
						foreach (var request in _pendingRequests)
							request.Value.TrySetException(ex);

						_onException(ex);
					}
				}
			}

			IEnumerable<BatchAppendReq> GetRequests(
				IEnumerable<MessageData> events,
				BatchAppendReq.Types.Options options,
				Uuid correlationId
			) {
				var batchSize        = 0;
				var first            = true;
				var correlationIdDto = correlationId.ToDto();
				var proposedMessages = new List<BatchAppendReq.Types.ProposedMessage>();

				foreach (var eventData in events) {
					var proposedMessage = new BatchAppendReq.Types.ProposedMessage {
						Data           = ByteString.CopyFrom(eventData.Data.Span),
						CustomMetadata = ByteString.CopyFrom(eventData.Metadata.InjectTracingContext(Activity.Current)),
						Id             = eventData.MessageId.ToDto(),
						Metadata = {
							{ Constants.Metadata.Type, eventData.Type },
							{ Constants.Metadata.ContentType, eventData.ContentType }
						}
					};

					proposedMessages.Add(proposedMessage);

					if ((batchSize += proposedMessage.CalculateSize()) < _settings.OperationOptions.BatchAppendSize)
						continue;

					yield return new BatchAppendReq {
						ProposedMessages = { proposedMessages },
						CorrelationId    = correlationIdDto,
						Options          = first ? options : null
					};

					first = false;
					proposedMessages.Clear();
					batchSize = 0;
				}

				yield return new BatchAppendReq {
					ProposedMessages = { proposedMessages },
					IsFinal          = true,
					CorrelationId    = correlationIdDto,
					Options          = first ? options : null
				};
			}

			public void Dispose() {
				_channel.Writer.TryComplete();
				_call?.Dispose();
			}
		}
	}

	public static class KurrentDBClientAppendToStreamExtensions {
		/// <summary>
		/// Appends events asynchronously to a stream. Messages are serialized using default or custom serialization configured through <see cref="KurrentDBClientSettings"/>
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to append events to.</param>
		/// <param name="expectedState">The expected <see cref="KurrentDB.Client.StreamState"/> of the stream to append to.</param>
		/// <param name="messages">Messages to append to the stream.</param>
		/// <param name="options">Optional settings for the append operation, e.g. expected stream position for optimistic concurrency check</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public static Task<IWriteResult> AppendToStreamAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			IEnumerable<object> messages,
			AppendToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		)
			=> dbClient.AppendToStreamAsync(
				streamName,
				expectedState,
				messages.Select(Message.From),
				options,
				cancellationToken
			);

		/// <summary>
		/// Appends events asynchronously to a stream.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to append events to.</param>	
		/// <param name="expectedState">The expected <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="eventData">An <see cref="IEnumerable{EventData}"/> to append to the stream.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{KurrentDBClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The <see cref="UserCredentials"/> for the operation.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete(
			"This method may be removed in future releases. Use the overload with Message for auto-serialization or method with options parameter for raw serialization",
			false
		)]
		public static Task<IWriteResult> AppendToStreamAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			Action<AppendToStreamOptions>? configureOperationOptions = null,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) {
			var operationOptions = new AppendToStreamOptions {
				Deadline        = deadline,
				UserCredentials = userCredentials,
			};

			configureOperationOptions?.Invoke(operationOptions);

			return dbClient.AppendToStreamAsync(
				streamName,
				expectedState,
				eventData.Select(e => (MessageData)e),
				operationOptions,
				cancellationToken
			);
		}
	}

	public class AppendToStreamOptions : OperationOptions {
		/// <summary>
		/// Whether or not to immediately throw a <see cref="WrongExpectedVersionException"/> when an append fails.
		/// </summary>
		public bool? ThrowOnAppendFailure { get; set; }

		/// <summary>
		/// The batch size, in bytes.
		/// </summary>
		public int? BatchAppendSize { get; set; }

		/// <summary>
		/// Clones a copy of the current <see cref="KurrentDBClientOperationOptions"/>.
		/// </summary>
		/// <returns></returns>
		public void With(KurrentDBClientOperationOptions clientOperationOptions) {
			ThrowOnAppendFailure ??= clientOperationOptions.ThrowOnAppendFailure;
			BatchAppendSize ??= clientOperationOptions.BatchAppendSize;
		}

		/// <summary>
		/// Allows to customize or disable the automatic deserialization
		/// </summary>
		public OperationSerializationSettings? SerializationSettings { get; set; }
	}
}
