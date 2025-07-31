#pragma warning disable CS8509

using EventStore.Client.Streams;
using Grpc.Core;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry.Serialization;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public partial class KurrentStreamsClient {
    internal KurrentStreamsClient(KurrentClient source) {
        source.TypeMapper.Map<StreamMetadata>("$metadata"); // metastream
        source.TypeMapper.Map<string>("$>");                // link

        SerializerProvider = source.SerializerProvider;
        MetadataDecoder    = source.MetadataDecoder;

        ServiceClient   = new StreamsServiceClient(source.LegacyCallInvoker);
        ServiceClientV1 = new Streams.StreamsClient(source.LegacyCallInvoker);
    }

    StreamsServiceClient  ServiceClient   { get; }
    Streams.StreamsClient ServiceClientV1 { get; }

    ISchemaSerializerProvider SerializerProvider { get; }
    IMetadataDecoder          MetadataDecoder    { get; }
}
