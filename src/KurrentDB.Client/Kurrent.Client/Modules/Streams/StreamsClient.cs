#pragma warning disable CS8509

using Kurrent.Client.Schema.Serialization;
using static KurrentDB.Protocol.Streams.V1.LegacyStreamsService;
using static KurrentDB.Protocol.Streams.V2.StreamsService;

namespace Kurrent.Client.Streams;

public partial class StreamsClient {
    internal StreamsClient(KurrentClient source) {
        source.TypeMapper.Map<StreamMetadata>("$metadata"); // metastream
        source.TypeMapper.Map<string>("$>");                // link

        SerializerProvider = source.SerializerProvider;
        MetadataDecoder    = source.MetadataDecoder;

        ServiceClient       = new StreamsServiceClient(source.LegacyCallInvoker);
        LegacyServiceClient = new LegacyStreamsServiceClient(source.LegacyCallInvoker);
    }

    StreamsServiceClient       ServiceClient       { get; }
    LegacyStreamsServiceClient LegacyServiceClient { get; }

    ISchemaSerializerProvider SerializerProvider { get; }
    IMetadataDecoder          MetadataDecoder    { get; }
}
