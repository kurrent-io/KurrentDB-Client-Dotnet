#pragma warning disable CS8509

using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(KurrentClient source, CallInvoker callInvoker) {
        source.TypeMapper.Map<StreamMetadata>("$metadata");
        source.TypeMapper.Map<string>("$>");

        SerializerProvider = source.SerializerProvider;
        MetadataDecoder    = source.MetadataDecoder;

        ServiceClient   = new StreamsServiceClient(callInvoker);
        ServiceClientV1 = new Streams.StreamsClient(callInvoker);
    }

    StreamsServiceClient      ServiceClient      { get; }
    Streams.StreamsClient     ServiceClientV1    { get; }

    ISchemaSerializerProvider SerializerProvider { get; }
    IMetadataDecoder          MetadataDecoder    { get; }
}
