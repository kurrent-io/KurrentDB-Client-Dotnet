using System.Text.Json;

namespace KurrentDB.Client;

/// <summary>
///  A set of extension methods for an <see cref="KurrentDBClient"/>.
/// </summary>
public static partial class KurrentDBClientExtensions {
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
	/// <param name="deadline"></param>
	/// <param name="userCredentials"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static Task SetSystemSettingsAsync(
		this KurrentDBClient dbClient,
		SystemSettings settings,
		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
		CancellationToken cancellationToken = default) {
		if (dbClient == null) throw new ArgumentNullException(nameof(dbClient));
		return dbClient.AppendToStreamAsync(SystemStreams.SettingsStream, StreamState.Any,
		[
			new EventData(Uuid.NewUuid(), SystemEventTypes.Settings,
					JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions))
		], deadline: deadline, userCredentials: userCredentials, cancellationToken: cancellationToken);
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
		this KurrentDBClient dbClient,
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
