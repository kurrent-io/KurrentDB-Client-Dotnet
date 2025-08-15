using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client;

public abstract class ModuleClientBase {
    protected ModuleClientBase(KurrentClient client, string? moduleName = null) {
        KurrentClient = client;
        Logger        = client.Options.LoggerFactory.CreateLogger(moduleName ?? GetType().Name);
    }

    protected KurrentClient KurrentClient { get; }
    protected ILogger       Logger        { get; }

    protected ISchemaSerializerProvider SerializerProvider => KurrentClient.SerializerProvider;
    protected ServerCapabilities        ServerCapabilities => KurrentClient.LegacyCallInvoker.ServerCapabilities;
    protected HttpClient                BackdoorClient     => KurrentClient.BackdoorClientFactory.GetClient();
    protected MessageTypeMapper         TypeMapper         => KurrentClient.Options.Mapper;
    protected IMetadataDecoder          MetadataDecoder    => KurrentClient.Options.MetadataDecoder;
}
