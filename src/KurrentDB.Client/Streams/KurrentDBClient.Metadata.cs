using System.Text.Json;
using EventStore.Client.Streams;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client {
	public partial class KurrentDBClient {
		/// <summary>
		/// Asynchronously reads the metadata for a stream
		/// </summary>
		/// <param name="streamName">The name of the stream to read the metadata for.</param>
		/// <param name="operationOptions"></param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public async Task<StreamMetadataResult> GetStreamMetadataAsync(
			string streamName,
			OperationOptions? operationOptions = null,
			CancellationToken cancellationToken = default
		) {
			_log.LogDebug("Read stream metadata for {streamName}.", streamName);

			try {
				var result = ReadStreamAsync(
					SystemStreams.MetastreamOf(streamName),
					new ReadStreamOptions {
						Direction             = Direction.Backwards,
						StreamPosition        = StreamPosition.End,
						ResolveLinkTos        = false,
						MaxCount              = 1,
						Deadline              = operationOptions?.Deadline,
						UserCredentials       = operationOptions?.UserCredentials,
						SerializationSettings = OperationSerializationSettings.Disabled
					},
					cancellationToken
				);

				await foreach (var message in
				               result.Messages.ConfigureAwait(false).WithCancellation(cancellationToken)) {
					if (message is not StreamMessage.Event(var resolvedEvent)) {
						continue;
					}

					return StreamMetadataResult.Create(
						streamName,
						resolvedEvent.OriginalEventNumber,
						JsonSerializer.Deserialize<StreamMetadata>(
							resolvedEvent.Event.Data.Span,
							StreamMetadataJsonSerializerOptions
						)
					);
				}
			} catch (StreamNotFoundException) { }

			_log.LogWarning("Stream metadata for {streamName} not found.", streamName);
			return StreamMetadataResult.None(streamName);
		}

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedState">The <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="operationOptions"></param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		public Task<IWriteResult> SetStreamMetadataAsync(
			string streamName,
			StreamState expectedState,
			StreamMetadata metadata,
			SetStreamMetadataOptions? operationOptions = null,
			CancellationToken cancellationToken = default
		) {
			operationOptions ??= new SetStreamMetadataOptions();
			operationOptions.With(Settings.OperationOptions);

			return SetStreamMetadataInternal(
				metadata,
				new AppendReq {
					Options = new AppendReq.Types.Options {
						StreamIdentifier = SystemStreams.MetastreamOf(streamName)
					}
				}.WithAnyStreamRevision(expectedState),
				operationOptions,
				cancellationToken
			);
		}

		async Task<IWriteResult> SetStreamMetadataInternal(
			StreamMetadata metadata,
			AppendReq appendReq,
			SetStreamMetadataOptions operationOptions,
			CancellationToken cancellationToken
		) {
			var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
			return await AppendToStreamInternal(
				channelInfo,
				appendReq,
				[
					new MessageData(
						SystemEventTypes.StreamMetadata,
						JsonSerializer.SerializeToUtf8Bytes(metadata, StreamMetadataJsonSerializerOptions)
					)
				],
				operationOptions,
				cancellationToken
			).ConfigureAwait(false);
		}
	}

	public static class KurrentDBClientMetadataObsoleteExtensions {
		/// <summary>
		/// Asynchronously reads the metadata for a stream
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to read the metadata for.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete("Use method with OperationOptions parameter")]
		public static Task<StreamMetadataResult> GetStreamMetadataAsync(
			this KurrentDBClient dbClient,
			string streamName,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.GetStreamMetadataAsync(
				streamName,
				new OperationOptions { Deadline = deadline, UserCredentials = userCredentials },
				cancellationToken
			);

		/// <summary>
		/// Asynchronously sets the metadata for a stream.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName">The name of the stream to set metadata for.</param>
		/// <param name="expectedState">The <see cref="StreamState"/> of the stream to append to.</param>
		/// <param name="metadata">A <see cref="StreamMetadata"/> representing the new metadata.</param>
		/// <param name="configureOperationOptions">An <see cref="Action{KurrentDBClientOperationOptions}"/> to configure the operation's options.</param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials">The optional <see cref="UserCredentials"/> to perform operation with.</param>
		/// <param name="cancellationToken">The optional <see cref="System.Threading.CancellationToken"/>.</param>
		/// <returns></returns>
		[Obsolete("Use method with SetStreamMetadataOptions parameter")]
		public static Task<IWriteResult> SetStreamMetadataAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			StreamMetadata metadata,
			Action<SetStreamMetadataOptions>? configureOperationOptions = null,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) {
			var operationOptions =
				new SetStreamMetadataOptions { Deadline = deadline, UserCredentials = userCredentials };
			configureOperationOptions?.Invoke(operationOptions);

			return dbClient.SetStreamMetadataAsync(
				streamName,
				expectedState,
				metadata,
				operationOptions,
				cancellationToken
			);
		}
	}

	public class SetStreamMetadataOptions : AppendToStreamOptions;
}
