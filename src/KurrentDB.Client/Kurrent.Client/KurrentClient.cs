using Kurrent.Client.Features;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;

namespace Kurrent.Client;

[PublicAPI]
public class KurrentClient : IAsyncDisposable {
	public static KurrentClientOptionsBuilder New => new KurrentClientOptionsBuilder();

    public KurrentClient(KurrentClientOptions options) {
	    options.EnsureOptionsAreValid();

	    Options = options;

        LegacyCallInvoker = new KurrentDBLegacyCallInvoker(
	        LegacyClusterClient.CreateWithExceptionMapping(options.ConvertToLegacySettings()));

        var schemaManager = new SchemaManager(
	        new KurrentRegistryClient(this),
	        NJsonSchemaExporter.Instance,
	        options.Mapper);

        SerializerProvider = new SchemaSerializerProvider([
	        new BytesPassthroughSerializer(),
	        new JsonSchemaSerializer(new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap }, schemaManager),
	        new ProtobufSchemaSerializer(new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap }, schemaManager)
        ]);

        Registry                = new KurrentRegistryClient(this);
        Streams                 = new KurrentStreamsClient(this);
        Operations              = new KurrentOperationsClient(this);
        PersistentSubscriptions = new KurrentPersistentSubscriptionsClient(this);
        Projections             = new KurrentProjectionsClient(this);
        Users                   = new KurrentUsersClient(this);
        Features                = new KurrentFeaturesClient(this);
    }

    internal KurrentClientOptions       Options            { get; }
    internal KurrentDBLegacyCallInvoker LegacyCallInvoker  { get; }
    internal ISchemaSerializerProvider  SerializerProvider { get; }

    internal MessageTypeMapper TypeMapper      => Options.Mapper;
    internal IMetadataDecoder  MetadataDecoder => Options.MetadataDecoder;

	public KurrentStreamsClient                 Streams                 { get; }
	public KurrentRegistryClient                Registry                { get; }
	public KurrentFeaturesClient                Features                { get; }
	public KurrentUsersClient                   Users                   { get; }
	public KurrentPersistentSubscriptionsClient PersistentSubscriptions { get; }
	public KurrentOperationsClient              Operations              { get; }
	public KurrentProjectionsClient             Projections             { get; }

	internal async Task<ServerFeatures> ForceRefresh(CancellationToken cancellationToken = default) {
		await LegacyCallInvoker.ForceRefresh(cancellationToken).ConfigureAwait(false);

		return new ServerFeatures {
			Version = LegacyCallInvoker.ServerCapabilities.Version
		};
	}

	public ValueTask DisposeAsync() =>
		LegacyCallInvoker.DisposeAsync();

    public static KurrentClient Create(KurrentClientOptions? options = null) =>
        new(options ?? new KurrentClientOptions());

    public static KurrentClient Create(string connectionString) =>
        Create(KurrentDBConnectionString.Parse(connectionString).ToClientOptions());
}
