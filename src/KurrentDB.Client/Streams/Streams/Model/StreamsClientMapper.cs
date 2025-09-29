// ReSharper disable InconsistentNaming

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Protocol.V2.Streams;
using KurrentDB.Protocol.V2.Streams.Errors;
using static KurrentDB.Client.Constants.Metadata;
using SchemaFormat = KurrentDB.Protocol.V2.Streams.SchemaFormat;

namespace KurrentDB.Client;

static class StreamsClientMapper {
	public static async IAsyncEnumerable<AppendRecord> Map(this IEnumerable<EventData> source) {
		foreach (var message in source)
			yield return await message
				.Map()
				.ConfigureAwait(false);
	}

	public static ValueTask<AppendRecord> Map(this EventData source) {
		Dictionary<string, object?> metadata = new();

		if (!source.Metadata.IsEmpty)
			metadata = MetadataDecoder.Decode(source.Metadata);

		metadata.InjectTracingContext(Activity.Current);

		var record = new AppendRecord {
			RecordId = source.EventId.ToString(),
			Data     = ByteString.CopyFrom(source.Data.Span),
			Schema = new SchemaInfo {
				Format = source.ContentType is ContentTypes.ApplicationJson
					? SchemaFormat.Json
					: SchemaFormat.Bytes,
				Name = source.Type
			},
			Properties = { metadata.MapToMapValue() }
		};

		return new ValueTask<AppendRecord>(record);
	}

	public static AppendResponse Map(this KurrentDB.Protocol.V2.Streams.AppendResponse source) => new(source.Stream, source.StreamRevision);

	public static IEnumerable<AppendResponse> Map(this RepeatedField<KurrentDB.Protocol.V2.Streams.AppendResponse> source) =>
		source.Select(response => response.Map());

	public static Exception MapRpcException(this RpcException ex) {
		var status = ex.GetRpcStatus();

		return ex.StatusCode switch {
			StatusCode.Aborted            => HandleAborted(ex, status),
			StatusCode.FailedPrecondition => HandleFailedPrecondition(ex, status),
			StatusCode.NotFound           => HandleNotFound(ex, status),
			StatusCode.InvalidArgument    => HandleInvalidArgument(ex, status),
			_                             => ex
		};
	}

	static Exception HandleFailedPrecondition(RpcException ex, Google.Rpc.Status? status) {
		var revisionConflict = status?.GetDetail<StreamRevisionConflictErrorDetails>();
		if (revisionConflict != null) {
			return new WrongExpectedVersionException(
				revisionConflict.Stream,
				StreamState.StreamRevision((ulong)revisionConflict.ExpectedRevision),
				StreamState.StreamRevision((ulong)revisionConflict.ActualRevision),
				ex
			);
		}

		var tombstoned = status?.GetDetail<StreamTombstonedErrorDetails>();
		if (tombstoned != null) return new StreamDeletedException(tombstoned.Stream, ex);

		return ex;
	}

	static Exception HandleNotFound(RpcException ex, Google.Rpc.Status? status) {
		var notFound = status?.GetDetail<StreamNotFoundErrorDetails>();
		if (notFound != null) return new StreamNotFoundException(notFound.Stream, ex);

		return ex;
	}

	static Exception HandleInvalidArgument(RpcException ex, Google.Rpc.Status? status) {
		var recordSizeExceeded = status?.GetDetail<AppendRecordSizeExceededErrorDetails>();
		if (recordSizeExceeded != null) return new MaximumAppendSizeExceededException(recordSizeExceeded.MaxSize, ex);

		return ex;
	}

	static Exception HandleAborted(RpcException ex, Google.Rpc.Status? status) {
		var transactionSizeExceeded = status?.GetDetail<AppendTransactionSizeExceededErrorDetails>();
		if (transactionSizeExceeded != null) return new TransactionMaxSizeExceededException(transactionSizeExceeded.MaxSize, ex);

		return ex;
	}
}
