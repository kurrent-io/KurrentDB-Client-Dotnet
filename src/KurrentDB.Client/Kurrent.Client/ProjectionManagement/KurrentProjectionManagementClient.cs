using EventStore.Client.Projections;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public sealed partial class KurrentProjectionManagementClient {
	internal KurrentProjectionManagementClient(CallInvoker callInvoker, KurrentClientOptions options) {
		Options = options;

		options.Mapper.Map<StreamMetadata>("$metadata");

		Registry = new KurrentRegistryClient(callInvoker);

		var schemaExporter = new SchemaExporter();
		var schemaManager  = new SchemaManager(Registry, schemaExporter, options.Mapper);

		SerializerProvider = new SchemaSerializerProvider(
			[
				new BytesPassthroughSerializer(),
				new JsonSchemaSerializer(
					new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
					schemaManager
				),
				new ProtobufSchemaSerializer(
					new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
					schemaManager
				)
			]
		);

		ServiceClient  = new Projections.ProjectionsClient(callInvoker);
		LegacySettings = options.ConvertToLegacySettings();
		LegacyClient   = new KurrentDBClient(LegacySettings);

		LegacyConverter = new KurrentDBLegacyConverter(
			SerializerProvider,
			options.MetadataDecoder,
			SchemaRegistryPolicy.NoRequirements
		);
	}

	internal KurrentClientOptions          Options            { get; }
	internal Projections.ProjectionsClient ServiceClient      { get; }
	internal KurrentRegistryClient         Registry           { get; }
	internal ISchemaSerializerProvider     SerializerProvider { get; }

	internal KurrentDBClientSettings  LegacySettings  { get; }
	internal KurrentDBClient          LegacyClient    { get; }
	internal KurrentDBLegacyConverter LegacyConverter { get; }
}
