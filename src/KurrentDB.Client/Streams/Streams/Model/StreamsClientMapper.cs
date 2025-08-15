#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using Google.Protobuf;
using Google.Protobuf.Collections;
using KurrentDB.Client.Schema.Serialization.Json;
using KurrentDB.Protocol.Streams.V2;

namespace KurrentDB.Client;

static class StreamsClientMapper {
	internal static JsonSerializer JsonSerializer { get; } = new();

	public static async IAsyncEnumerable<AppendRecord> Map(this IEnumerable<EventData> source) {
		foreach (var message in source)
			yield return await message
				.Map()
				.ConfigureAwait(false);
	}

	public static ValueTask<AppendRecord> Map(this EventData source) {
		Dictionary<string, object?> metadata;

		if (source.Metadata.IsEmpty) {
			metadata = new();
		} else {
			try {
				metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(source.Metadata) ?? new();
			} catch (Exception ex) {
				throw new ArgumentException(
					$"Event metadata must be valid JSON with property values limited to: null, boolean, number, string, Guid, DateTime, TimeSpan, or Base64-encoded byte arrays. " +
					$"Complex objects and arrays are not supported. This limitation will be removed in the next major release. " +
					$"Deserialization failed: {ex.Message}",
					nameof(source),
					ex
				);
			}
		}

		metadata[Constants.Metadata.SchemaName] = source.Type;
		metadata[Constants.Metadata.SchemaDataFormat] = source.ContentType is Constants.Metadata.ContentTypes.ApplicationJson
			? SchemaDataFormat.Json
			: SchemaDataFormat.Bytes;

		var record = new AppendRecord {
			RecordId   = source.EventId.ToString(),
			Data       = ByteString.CopyFrom(source.Data.Span),
			Properties = { metadata.MapToDynamicMapField() }
		};

		return new ValueTask<AppendRecord>(record);
	}

	public static Exception Map(this AppendStreamFailure source) {
		return source.ErrorCase switch {
			AppendStreamFailure.ErrorOneofCase.StreamRevisionConflict => new WrongExpectedVersionException(
				source.Stream,
				StreamState.StreamRevision((ulong)source.StreamRevisionConflict.StreamRevision)
			),
			AppendStreamFailure.ErrorOneofCase.AccessDenied               => new AccessDeniedException(),
			AppendStreamFailure.ErrorOneofCase.StreamDeleted              => new StreamDeletedException(source.Stream),
			AppendStreamFailure.ErrorOneofCase.TransactionMaxSizeExceeded => new TransactionMaxSizeExceededException(source.TransactionMaxSizeExceeded.MaxSize),
		};
	}

	public static AppendStreamSuccess Map(this Protocol.Streams.V2.AppendStreamSuccess source) =>
		new(source.Stream, source.Position);

	public static AppendStreamFailures Map(this RepeatedField<AppendStreamFailure> source) =>
		new(source.Select(failure => failure.Map()));

	public static AppendStreamSuccesses Map(this RepeatedField<Protocol.Streams.V2.AppendStreamSuccess> source) =>
		new(source.Select(success => success.Map()));

	public static AppendStreamFailures Map(this MultiStreamAppendResponse.Types.Failure source) =>
		new(source.Output.Map());

	public static AppendStreamSuccesses Map(this MultiStreamAppendResponse.Types.Success source) =>
		new(source.Output.Map());
}
