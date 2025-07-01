#pragma warning disable CS8509

using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(CallInvoker callInvoker, KurrentClientOptions options) {
        Options  = options;

        options.Mapper.Map<StreamMetadata>("$metadata");

        ServiceClient = new StreamsServiceClient(callInvoker);
        Registry      = new KurrentRegistryClient(callInvoker);

        var schemaExporter = new SchemaExporter();
        var schemaManager  = new SchemaManager(Registry, schemaExporter, options.Mapper);

        SerializerProvider = new SchemaSerializerProvider([
            new BytesPassthroughSerializer(),
            new JsonSchemaSerializer(
                new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager
            ),
            new ProtobufSchemaSerializer(
                new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
                schemaManager
            )
        ]);

        LegacyStreamsClient = new Streams.StreamsClient(callInvoker);
        // LegacySettings      = options.ConvertToLegacySettings();
        // LegacyClient        = new KurrentDBClient(LegacySettings);

        LegacyConverter = new KurrentDBLegacyConverter(
            SerializerProvider,
            options.MetadataDecoder,
            SchemaRegistryPolicy.NoRequirements
        );
    }

    internal KurrentClientOptions      Options            { get; }
    internal StreamsServiceClient      ServiceClient      { get; }
    internal KurrentRegistryClient     Registry           { get; }
    internal ISchemaSerializerProvider SerializerProvider { get; }

    // internal KurrentDBClientSettings  LegacySettings      { get; }
    // internal KurrentDBClient          LegacyClient        { get; }
    internal KurrentDBLegacyConverter LegacyConverter     { get; }
    internal Streams.StreamsClient    LegacyStreamsClient { get; }
}
