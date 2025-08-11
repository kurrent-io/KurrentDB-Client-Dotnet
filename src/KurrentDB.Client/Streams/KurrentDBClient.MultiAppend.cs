// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable PossibleMultipleEnumeration

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using JetBrains.Annotations;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using static KurrentDB.Protocol.Streams.V2.MultiStreamAppendResponse;
using static KurrentDB.Client.Constants;
using Contracts = KurrentDB.Protocol.Streams.V2;
using JsonSerializer = KurrentDB.Client.Schema.Serialization.Json.JsonSerializer;

namespace KurrentDB.Client;

[PublicAPI]
public record AppendStreamRequest(string Stream, StreamState ExpectedState, IEnumerable<EventData> Messages);

[PublicAPI]
public abstract class MultiAppendWriteResult {
	public abstract bool IsSuccess { get; }
	public          bool IsFailure => !IsSuccess;
}

[PublicAPI]
public sealed class MultiAppendSuccess : MultiAppendWriteResult {
	public override bool                  IsSuccess => true;
	public          AppendStreamSuccesses Successes { get; }

	internal MultiAppendSuccess(AppendStreamSuccesses successes) {
		Successes = successes;
	}
}

[PublicAPI]
public sealed class MultiAppendFailure : MultiAppendWriteResult {
	public override bool                 IsSuccess => false;
	public          AppendStreamFailures Failures  { get; }

	internal MultiAppendFailure(AppendStreamFailures failures) {
		Failures = failures;
	}
}

public partial class KurrentDBClient {
	/// <summary>
	/// Appends events to multiple streams asynchronously, ensuring the specified state of each stream is respected.
	/// </summary>
	/// <param name="requests">An asynchronous enumerable of <see cref="AppendStreamRequest"/> objects, each containing details of the stream, expected stream state, and events to append.</param>
	/// <param name="cancellationToken">An optional cancellation token to observe while waiting for the operation to complete.</param>
	/// <returns>
	/// A task that represents the asynchronous operation, with a result of type <see cref="MultiAppendWriteResult"/>, indicating the outcome of the operation.
	/// <para>
	/// On success, returns <see cref="MultiAppendSuccess"/> containing the successful append results.
	/// On failure, returns <see cref="MultiAppendFailure"/> containing a collection of exceptions that may include:
	/// <see cref="WrongExpectedVersionException"/>, <see cref="AccessDeniedException"/>, <see cref="StreamDeletedException"/>,
	/// <see cref="StreamNotFoundException"/>, or <see cref="TransactionMaxSizeExceededException"/>.
	/// </para>
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if the server does not support multi-stream append functionality (requires server version 25.1 or higher).</exception>
	public async ValueTask<MultiAppendWriteResult> MultiStreamAppendAsync(
		IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

		if (!channelInfo.ServerCapabilities.SupportsMultiStreamAppend)
			throw new InvalidOperationException($"{nameof(MultiStreamAppendAsync)} requires server version 25.1 or higher.");

		var client = new StreamsServiceClient(channelInfo.CallInvoker);

		using var session = client.MultiStreamAppendSession(KurrentDBCallOptions.CreateStreaming(Settings, cancellationToken: cancellationToken));

		var observables = KurrentDBClientDiagnostics.ActivitySource.InstrumentAppendOperations(requests, CreateActivityTags);

		await foreach (var (activity, request) in observables.WithCancellation(cancellationToken)) {
			var records = await request.Messages
				.Map(activity)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var serviceRequest = new Contracts.AppendStreamRequest {
				Stream           = request.Stream,
				ExpectedRevision = request.ExpectedState.ToInt64(),
				Records          = { records }
			};

			// Cancellation of stream writes is not supported by this gRPC implementation.
			// To cancel the operation, we should cancel the entire session.
			await session.RequestStream
				.WriteAsync(serviceRequest)
				.ConfigureAwait(false);
		}

		await session.RequestStream.CompleteAsync();

		var response = await session.ResponseAsync;

		await KurrentDBClientDiagnostics.ActivitySource.CompleteAppendInstrumentation(observables, response);

		return response.ResultCase switch {
			ResultOneofCase.Success => new MultiAppendSuccess(response.Success.Map()),
			ResultOneofCase.Failure => new MultiAppendFailure(response.Failure.Map())
		};

		ActivityTagsCollection CreateActivityTags(AppendStreamRequest request) =>
			new ActivityTagsCollection()
				.WithRequiredTag(TelemetryTags.KurrentDB.Stream, request.Stream)
				.WithGrpcChannelServerTags(channelInfo)
				.WithClientSettingsServerTags(Settings)
				.WithOptionalTag(TelemetryTags.Database.User, Settings.DefaultCredentials?.Username);
	}
}

static class Mapper {
	internal static JsonSerializer JsonSerializer { get; } = new();

	public static async IAsyncEnumerable<Contracts.AppendRecord> Map(this IEnumerable<EventData> source, Activity? activity = null) {
		foreach (var message in source)
			yield return await message
				.Map(activity)
				.ConfigureAwait(false);
	}

	public static ValueTask<Contracts.AppendRecord> Map(this EventData source, Activity? activity = null) {
		Dictionary<string, object?> metadata;

		if (source.Metadata.IsEmpty) {
			metadata = new();
		} else {
			try {
				metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(source.Metadata) ?? new();
			} catch (Exception ex) {
				throw new ArgumentException(
					$"Event metadata must be valid JSON that can be deserialized to Dictionary<string, object?>. This limitation will be removed in the next major release" +
					$"Deserialization failed: {ex.Message}",
					nameof(source),
					ex
				);
			}
		}

		metadata[Metadata.SchemaName] = source.Type;
		metadata[Metadata.SchemaDataFormat] = source.ContentType is Metadata.ContentTypes.ApplicationJson
			? SchemaDataFormat.Json
			: SchemaDataFormat.Bytes;

		metadata.InjectTracingContext(activity);

		var record = new Contracts.AppendRecord {
			RecordId   = source.EventId.ToString(),
			Data       = ByteString.CopyFrom(source.Data.Span),
			Properties = { metadata.MapToDynamicMapField() }
		};

		return new ValueTask<Contracts.AppendRecord>(record);
	}

	public static Exception Map(this Contracts.AppendStreamFailure source) {
		return source.ErrorCase switch {
			Contracts.AppendStreamFailure.ErrorOneofCase.StreamRevisionConflict => new WrongExpectedVersionException(
				source.Stream,
				StreamState.StreamRevision((ulong)source.StreamRevisionConflict.StreamRevision)
			),
			Contracts.AppendStreamFailure.ErrorOneofCase.AccessDenied   => new AccessDeniedException(),
			Contracts.AppendStreamFailure.ErrorOneofCase.StreamDeleted  => new StreamDeletedException(source.Stream),
			Contracts.AppendStreamFailure.ErrorOneofCase.TransactionMaxSizeExceeded => new TransactionMaxSizeExceededException(
				source.TransactionMaxSizeExceeded.MaxSize
			),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	public static AppendStreamSuccess Map(this Contracts.AppendStreamSuccess source) =>
		new(source.Stream, source.Position);

	public static AppendStreamFailures Map(this RepeatedField<Contracts.AppendStreamFailure> source) =>
		new(source.Select(failure => failure.Map()));

	public static AppendStreamSuccesses Map(this RepeatedField<Contracts.AppendStreamSuccess> source) =>
		new(source.Select(success => success.Map()));

	public static AppendStreamFailures Map(this Types.Failure source) =>
		new(source.Output.Map());

	public static AppendStreamSuccesses Map(this Types.Success source) =>
		new(source.Output.Map());
}

[PublicAPI]
public record AppendStreamSuccess(string Stream, long Position);

[PublicAPI]
public class AppendStreamSuccesses : List<AppendStreamSuccess> {
	public AppendStreamSuccesses() { }
	public AppendStreamSuccesses(IEnumerable<AppendStreamSuccess> input) : base(input) { }
}

[PublicAPI]
public class AppendStreamFailures : List<Exception> {
	public AppendStreamFailures() { }
	public AppendStreamFailures(IEnumerable<Exception> input) : base(input) { }
}
