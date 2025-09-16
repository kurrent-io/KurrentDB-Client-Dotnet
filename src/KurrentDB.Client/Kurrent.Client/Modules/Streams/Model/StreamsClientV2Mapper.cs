#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using KurrentDB.Diagnostics.Tracing;
using Contracts = KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client.Streams;

static class StreamsClientV2Mapper {
	public static async IAsyncEnumerable<Contracts.AppendRecord> Map(this IEnumerable<Message> source, string stream, ISchemaSerializerProvider serializerProvider, [EnumeratorCancellation] CancellationToken ct) {
		foreach (var message in source)
			yield return await message
				.Map(stream, serializerProvider, ct)
				.ConfigureAwait(false);
	}

	public static async ValueTask<Contracts.AppendRecord> Map(this Message source, string stream, ISchemaSerializerProvider serializerProvider, CancellationToken ct) {
		var context = new SchemaSerializationContext {
			Stream               = stream,
			Metadata             = source.Metadata,
			SchemaRegistryPolicy = SchemaRegistryPolicy.NoRequirements,
			CancellationToken    = ct
		};

		var data = await serializerProvider
			.GetSerializer(source.DataFormat)
			.Serialize(source.Value, context)
			.ConfigureAwait(false);

		source.Metadata
			.With(TraceConstants.TraceId, Activity.Current?.TraceId)
			.With(TraceConstants.SpanId, Activity.Current?.SpanId);

		return new Contracts.AppendRecord {
			RecordId   = source.RecordId.ToString(),
			// Data       = ByteString.CopyFrom(data.Span),
			Data       = UnsafeByteOperations.UnsafeWrap(data), // Constructs a new ByteString from the given bytes. The bytes are not copied, and must not be modified while the ByteString is in use.
			Properties = { source.Metadata.MapToDynamicMapField() }
		};
	}

	public static async Task<Record> Map(this Contracts.Record source, ISchemaSerializerProvider serializerProvider, bool skipDecoding,  CancellationToken ct) {
		var metadata = source.Properties.MapToMetadata();

		// create a decoder
		IRecordDecoder decoder = new RecordDecoder(serializerProvider);

		// and we are done
		var record = new Record(decoder) {
			Id             = Guid.Parse(source.RecordId),
			Stream         = source.Stream,
			StreamRevision = source.StreamRevision,
			Position       = source.Position,
			Timestamp      = source.Timestamp.ToDateTime(),
			Metadata       = metadata,
			Data           = source.Data.Memory // .ToByteArray() ? safer?
		};

		// now decode the record if required
		if (!skipDecoding)
			await record
				.TryDecode(ct)
				.ConfigureAwait(false);

		return record;
	}

	public static Contracts.ReadDirection Map(this ReadDirection source) =>
		source switch {
			ReadDirection.Forwards  => Contracts.ReadDirection.Forwards,
			ReadDirection.Backwards => Contracts.ReadDirection.Backwards,
			_                       => throw new UnreachableException($"Unexpected read direction: {source}")
		};

	public static Contracts.ReadFilter Map(this ReadFilter source) =>
		new() {
			Scope      = source.Scope.Map(),
			Expression = source.Expression
		};

	public static Contracts.ReadFilterScope Map(this ReadFilterScope source) =>
		source switch {
			ReadFilterScope.Unspecified => Contracts.ReadFilterScope.Unspecified,
			ReadFilterScope.Stream      => Contracts.ReadFilterScope.Stream,
			ReadFilterScope.SchemaName  => Contracts.ReadFilterScope.SchemaName,
			ReadFilterScope.Properties  => Contracts.ReadFilterScope.Properties,
			ReadFilterScope.Record      => Contracts.ReadFilterScope.Record,
			_                           => throw new UnreachableException($"Unexpected read filter scope: {source}")
		};

	public static Heartbeat Map(this Contracts.Heartbeat source) {
		return new Heartbeat(
			source.Type switch {
				Contracts.HeartbeatType.Checkpoint => HeartbeatType.Checkpoint,
				Contracts.HeartbeatType.CaughtUp   => HeartbeatType.CaughtUp,
				Contracts.HeartbeatType.FellBehind => HeartbeatType.FellBehind,
				_                                  => throw new UnreachableException($"Unexpected heartbeat type: {source.Type}")
			},
			source.Position,
			StreamRevision.Unset,
			source.Timestamp.ToDateTimeOffset()
		);
	}

	public static Contracts.HeartbeatOptions Map(this HeartbeatOptions source) =>
		new() {
			Enable           = source.Enable,
			RecordsThreshold = source.RecordsThreshold
		};

    public static AppendStreamFailure Map(this Contracts.AppendStreamFailure source) {
        return source.ErrorCase switch {
            Contracts.AppendStreamFailure.ErrorOneofCase.StreamRevisionConflict     => new ErrorDetails.StreamRevisionConflict(meta => meta.With("Stream", source.Stream).With("ExpectedRevision", source.StreamRevisionConflict.StreamRevision)),
            Contracts.AppendStreamFailure.ErrorOneofCase.AccessDenied               => new ErrorDetails.AccessDenied(meta => meta.With("Stream", source.Stream)),
            Contracts.AppendStreamFailure.ErrorOneofCase.StreamDeleted              => new ErrorDetails.StreamTombstoned(meta => meta.With("Stream", source.Stream)),
            Contracts.AppendStreamFailure.ErrorOneofCase.TransactionMaxSizeExceeded => new ErrorDetails.TransactionMaxSizeExceeded(meta => meta.With("MaxSize", source.TransactionMaxSizeExceeded.MaxSize) ),
            _                                                                       => throw new UnreachableException($"Unexpected append stream failure error category: {source.ErrorCase}")
        };
    }

    public static AppendStreamSuccess Map(this Contracts.AppendStreamSuccess source) =>
		new(source.Stream, source.Position, source.StreamRevision);

	public static AppendStreamFailures Map(this RepeatedField<Contracts.AppendStreamFailure> source) =>
		new(source.Select(failure => failure.Map()));

	public static AppendStreamSuccesses Map(this RepeatedField<Contracts.AppendStreamSuccess> source) =>
		new(source.Select(success => success.Map()));

	public static AppendStreamFailures Map(this Contracts.MultiStreamAppendResponse.Types.Failure source) =>
		new(source.Output.Map());

	public static AppendStreamSuccesses Map(this Contracts.MultiStreamAppendResponse.Types.Success source) =>
		new(source.Output.Map());
}
