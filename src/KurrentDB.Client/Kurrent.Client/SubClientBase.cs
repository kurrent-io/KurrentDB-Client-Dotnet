using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client;

public abstract class SubClientBase {
    protected SubClientBase(KurrentClient client) {
        KurrentClient = client;
        Logger        = client.Options.LoggerFactory.CreateLogger(GetType().Name);
    }

    protected KurrentClient KurrentClient { get; }
    protected ILogger       Logger        { get; }

    protected ISchemaSerializerProvider SerializerProvider => KurrentClient.SerializerProvider;
    protected ServerCapabilities        ServerCapabilities => KurrentClient.LegacyCallInvoker.ServerCapabilities;
    protected HttpClient                BackdoorClient     => KurrentClient.LegacyCallInvoker.ChannelOptions.HttpClient!;
    protected MessageTypeMapper         TypeMapper         => KurrentClient.Options.Mapper;
    protected IMetadataDecoder          MetadataDecoder    => KurrentClient.Options.MetadataDecoder;
}
