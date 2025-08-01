#pragma warning disable CS8509

using Kurrent.Client.Model;
using Kurrent.Client.Schema.Serialization;
using static KurrentDB.Protocol.Streams.V2.StreamsService;
using StreamMetadata = Kurrent.Client.Streams.StreamMetadata;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    internal StreamsClient(KurrentClient source) {
        source.TypeMapper.Map<StreamMetadata>("$metadata"); // metastream
        source.TypeMapper.Map<string>("$>");                // link

        SerializerProvider = source.SerializerProvider;
        MetadataDecoder    = source.MetadataDecoder;

        ServiceClient   = new StreamsServiceClient(source.LegacyCallInvoker);
        ServiceClientV1 = new EventStore.Client.Streams.Streams.StreamsClient(source.LegacyCallInvoker);
    }

    StreamsServiceClient                            ServiceClient   { get; }
    EventStore.Client.Streams.Streams.StreamsClient ServiceClientV1 { get; }

    ISchemaSerializerProvider SerializerProvider { get; }
    IMetadataDecoder          MetadataDecoder    { get; }
}
