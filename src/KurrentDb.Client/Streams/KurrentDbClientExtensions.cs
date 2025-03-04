using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace KurrentDb.Client {
	/// <summary>
	///  A set of extension methods for an <see cref="KurrentDbClient"/>.
	/// </summary>
	public static class KurrentDbClientExtensions {
		private static readonly JsonSerializerOptions SystemSettingsJsonSerializerOptions = new JsonSerializerOptions {
			Converters = {
				SystemSettingsJsonConverter.Instance
			},
		};

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
			this KurrentDbClient dbClient,
			SystemSettings settings,
			TimeSpan? deadline = null, UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (dbClient == null) throw new ArgumentNullException(nameof(dbClient));
			return dbClient.AppendToStreamAsync(SystemStreams.SettingsStream, StreamState.Any,
				new[] {
					new EventData(Uuid.NewUuid(), SystemEventTypes.Settings,
						JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions))
				}, deadline: deadline, userCredentials: userCredentials, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Appends to a stream conditionally.
		/// </summary>
		/// <param name="dbClient"></param>
		/// <param name="streamName"></param>
		/// <param name="expectedRevision"></param>
		/// <param name="eventData"></param>
		/// <param name="deadline"></param>
		/// <param name="userCredentials"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this KurrentDbClient dbClient,
			string streamName,
			StreamRevision expectedRevision,
			IEnumerable<EventData> eventData,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (dbClient == null) {
				throw new ArgumentNullException(nameof(dbClient));
			}
			try {
				var result = await dbClient.AppendToStreamAsync(streamName, expectedRevision, eventData,
						options => options.ThrowOnAppendFailure = false, deadline, userCredentials, cancellationToken)
					.ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
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
		public static async Task<ConditionalWriteResult> ConditionalAppendToStreamAsync(
			this KurrentDbClient dbClient,
			string streamName,
			StreamState expectedState,
			IEnumerable<EventData> eventData,
			TimeSpan? deadline = null,
			UserCredentials? userCredentials = null,
			CancellationToken cancellationToken = default) {
			if (dbClient == null) {
				throw new ArgumentNullException(nameof(dbClient));
			}
			try {
				var result = await dbClient.AppendToStreamAsync(streamName, expectedState, eventData,
						options => options.ThrowOnAppendFailure = false, deadline, userCredentials, cancellationToken)
					.ConfigureAwait(false);
				return ConditionalWriteResult.FromWriteResult(result);
			} catch (StreamDeletedException) {
				return ConditionalWriteResult.StreamDeleted;
			} catch (WrongExpectedVersionException ex) {
				return ConditionalWriteResult.FromWrongExpectedVersion(ex);
			}
		}
	}
}
