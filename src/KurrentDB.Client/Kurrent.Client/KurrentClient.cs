using Kurrent.Client.Features;
using Kurrent.Client.Legacy;
using Kurrent.Client.Projections;
using Kurrent.Client.Registry;
using Kurrent.Client.Schema;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Schema.Serialization.Bytes;
using Kurrent.Client.Schema.Serialization.Json;
using Kurrent.Client.Schema.Serialization.Protobuf;
using Kurrent.Client.Streams;
using Kurrent.Client.Users;
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
	        new RegistryClient(this),
	        NJsonSchemaExporter.Instance,
	        options.Mapper);

        SerializerProvider = new SchemaSerializerProvider([
	        new BytesPassthroughSerializer(),
	        new JsonSchemaSerializer(new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap }, schemaManager),
	        new ProtobufSchemaSerializer(new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap }, schemaManager)
        ]);

        Registry                = new RegistryClient(this);
        Streams                 = new StreamsClient(this);
        Operations              = new Operations.OperationsClient(this);
        PersistentSubscriptions = new KurrentPersistentSubscriptionsClient(this);
        Projections             = new ProjectionsClient(this);
        Users                   = new UsersClient(this);
        Features                = new FeaturesClient(this);
    }

    internal KurrentClientOptions       Options            { get; }
    internal KurrentDBLegacyCallInvoker LegacyCallInvoker  { get; }
    internal ISchemaSerializerProvider  SerializerProvider { get; }

    internal MessageTypeMapper TypeMapper      => Options.Mapper;
    internal IMetadataDecoder  MetadataDecoder => Options.MetadataDecoder;

    public StreamsClient                Streams                 { get; }
    public RegistryClient                       Registry                { get; }
    public FeaturesClient                       Features                { get; }
    public UsersClient                          Users                   { get; }
    public KurrentPersistentSubscriptionsClient PersistentSubscriptions { get; }
    public Operations.OperationsClient          Operations              { get; }
    public ProjectionsClient                    Projections             { get; }

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
