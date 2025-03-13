using System.Text.Json;

namespace KurrentDB.Client {
	/// <summary>
	///  A set of extension methods for an <see cref="KurrentDBClient"/>.
	/// </summary>
	public static class KurrentDBClientExtensions {
		static readonly JsonSerializerOptions SystemSettingsJsonSerializerOptions = new JsonSerializerOptions {
			Converters = {
				SystemSettingsJsonConverter.Instance
			},
		};

		/// <summary>
		/// Writes <see cref="SystemSettings"/> to the $settings stream.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="settings"></param>
		/// <param name="options">Optional settings for the append operation, e.g. deadline, user credentials etc.</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static Task SetSystemSettingsAsync(
			this KurrentDBClient dbClient,
			SystemSettings settings,
			SetSystemSettingsOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			if (dbClient == null) throw new ArgumentNullException(nameof(dbClient));

			return dbClient.AppendToStreamAsync(
				SystemStreams.SettingsStream,
				StreamState.Any,
				[
					new MessageData(
						SystemEventTypes.Settings,
						JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions)
					)
				],
				options,
				cancellationToken
			);
		}

		/// <summary>
		/// Writes <see cref="SystemSettings"/> to the $settings stream.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="settings"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static Task SetSystemSettingsAsync(
			this KurrentDBClient dbClient,
			SystemSettings settings,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.SetSystemSettingsAsync(
				settings,
				new SetSystemSettingsOptions { Deadline = deadline, UserCredentials = userCredentials },
				cancellationToken: cancellationToken
			);

		/// <summary>
		/// Appends to a stream conditionally.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName"></param>
		/// <param name="expectedState"></param>
		/// <param name="messageData"></param>
		/// <param name="options"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
			IEnumerable<MessageData> messageData,
			AppendToStreamOptions? options = null,
			CancellationToken cancellationToken = default
		) {
			if (dbClient == null) {
				throw new ArgumentNullException(nameof(dbClient));
			}

			try {
				var result = await dbClient.AppendToStreamAsync(
						streamName,
						expectedState,
						messageData,
						options,
						cancellationToken
					)
					.ConfigureAwait(false);

				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			} catch (WrongExpectedVersionException ex) {
				return ConditionalWriteResult.FromWrongExpectedVersion(ex);
			}
		}

		/// <summary>
		/// Appends to a stream conditionally.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName"></param>
		/// <param name="expectedState"></param>
		/// <param name="eventData"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this KurrentDBClient dbClient,
			string streamName,
			StreamState expectedState,
#pragma warning disable CS0618 // Type or member is obsolete
			IEnumerable<EventData> eventData,
#pragma warning restore CS0618 // Type or member is obsolete
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default
		) =>
			dbClient.ConditionalAppendToStreamAsync(
				streamName,
				expectedState,
				eventData.Select(e => (MessageData)e),
				new AppendToStreamOptions {
					ThrowOnAppendFailure = false,
					Deadline             = deadline,
					UserCredentials      = userCredentials
				},
				cancellationToken
			);
	}

	public class SetSystemSettingsOptions : AppendToStreamOptions;
}
