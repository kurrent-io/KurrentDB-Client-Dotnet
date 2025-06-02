#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using KurrentDB.Client;

using Contracts = KurrentDB.Protocol.Streams.V2;

namespace Kurrent.Client.Model;

static partial class Mapping {
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
			.Serialize(source, context)
			.ConfigureAwait(false);

		// // we need to remove the schema name from the
		// // metadata as it is not required in the end.
		// message.Metadata.Remove(SystemMetadataKeys.SchemaName);

		return new Contracts.AppendRecord {
			RecordId   = Uuid.FromGuid(source.RecordId).ToString(), // not sure if this is still relevant, but keeping it for now.
			Data       = ByteString.CopyFrom(data.Span),
			Properties = { source.Metadata.MapToDynamicMapField() }
		};
	}

	public static async Task<Record> Map(this Contracts.Record source, ISchemaSerializerProvider serializerProvider,  CancellationToken cancellationToken) {
		var metadata = source.Properties.MapToMetadata();

		var context = new SchemaSerializationContext {
			Stream               = source.Stream,
			Metadata             = metadata,
			SchemaRegistryPolicy = SchemaRegistryPolicy.NoRequirements,
			CancellationToken    = cancellationToken
		};

		var value = await serializerProvider
			.GetSerializer(metadata.GetSchemaDataFormat())
			.Deserialize(source.Data.Memory, context)
			.ConfigureAwait(false);

		return new Record {
			Id             = Guid.Parse(source.RecordId),
			Position       = source.Position,
			Stream         = source.Stream,
			StreamRevision = source.StreamRevision,
			Timestamp      = source.Timestamp.ToDateTime(),
			Metadata       = metadata,
			Value          = value!,
			ValueType      = value!.GetType(),
			Data           = source.Data.ToByteArray()
		};
	}

	public static Contracts.ReadDirection Map(this Direction source) =>
		source switch {
			Direction.Forwards  => Contracts.ReadDirection.Forwards,
			Direction.Backwards => Contracts.ReadDirection.Backwards,
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
			_                           => throw new ArgumentOutOfRangeException(nameof(source), source, null)
		};

	public static Heartbeat Map(this Contracts.Heartbeat source) {
		return new Heartbeat(
			source.Type switch {
				Contracts.HeartbeatType.Checkpoint => HeartbeatType.Checkpoint,
				Contracts.HeartbeatType.CaughtUp   => HeartbeatType.CaughtUp,
				_                                  => throw new UnreachableException($"Unexpected heartbeat type: {source.Type}")
			},
			source.Position,
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
			Contracts.AppendStreamFailure.ErrorOneofCase.StreamNotFound             => new AppendErrorDetails.StreamNotFound(source.Stream),
			Contracts.AppendStreamFailure.ErrorOneofCase.StreamDeleted              => new AppendErrorDetails.StreamDeleted(source.Stream),
			Contracts.AppendStreamFailure.ErrorOneofCase.WrongExpectedRevision      => new AppendErrorDetails.WrongExpectedRevision(source.Stream, source.WrongExpectedRevision.StreamRevision),
			Contracts.AppendStreamFailure.ErrorOneofCase.AccessDenied               => new AppendErrorDetails.AccessDenied(),
			Contracts.AppendStreamFailure.ErrorOneofCase.TransactionMaxSizeExceeded => new AppendErrorDetails.TransactionMaxSizeExceeded(source.TransactionMaxSizeExceeded.MaxSize),
			_                                                                       => throw new UnreachableException($"Unexpected append stream failure error case: {source.ErrorCase}")
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
