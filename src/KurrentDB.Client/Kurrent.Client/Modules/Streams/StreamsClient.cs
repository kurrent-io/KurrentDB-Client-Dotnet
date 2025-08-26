using LegacyStreamsServiceClient = KurrentDB.Protocol.Streams.V1.Streams.StreamsClient;
using static KurrentDB.Protocol.Streams.V2.StreamsService;

namespace Kurrent.Client.Streams;

public partial class StreamsClient : ClientModuleBase {
    internal StreamsClient(KurrentClient client) : base(client) {
        TypeMapper.Map<StreamMetadata>("$metadata"); // metastream
        TypeMapper.Map<string>("$>");                // link

        ServiceClient       = new StreamsServiceClient(client.LegacyCallInvoker);
        LegacyServiceClient = new LegacyStreamsServiceClient(client.LegacyCallInvoker);
    }

    StreamsServiceClient       ServiceClient       { get; }
    LegacyStreamsServiceClient LegacyServiceClient { get; }
}
