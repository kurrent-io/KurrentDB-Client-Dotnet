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
public class KurrentClient : IAsyncDisposable, IKurrentClient {
	public static KurrentClientOptionsBuilder New => new KurrentClientOptionsBuilder();

    public KurrentClient(KurrentClientOptions options) {
        options.EnsureOptionsAreValid();

        TypeMapper = options.Mapper;

        LegacyCallInvoker = new KurrentDBLegacyCallInvoker(
	        LegacyClusterClient.CreateWithExceptionMapping(options.ConvertToLegacySettings()));

        MetadataDecoder = options.MetadataDecoder;

        var schemaManager = new SchemaManager(
	        new KurrentRegistryClient(LegacyCallInvoker),
	        NJsonSchemaExporter.Instance,
	        options.Mapper);

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

        Streams  = new KurrentStreamsClient(this, LegacyCallInvoker);
        Registry = new KurrentRegistryClient(LegacyCallInvoker);
        Features = new KurrentFeaturesClient(LegacyCallInvoker);
    }

    KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }

    public MessageTypeMapper         TypeMapper         { get; }
    public ISchemaSerializerProvider SerializerProvider { get; }
    public IMetadataDecoder          MetadataDecoder    { get; }


	public KurrentStreamsClient  Streams  { get; }
	public KurrentRegistryClient Registry { get; }
	public KurrentFeaturesClient Features { get; }

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

public interface IKurrentClient {
	ISchemaSerializerProvider SerializerProvider { get; }
	IMetadataDecoder          MetadataDecoder    { get; }
	MessageTypeMapper         TypeMapper         { get; }

	KurrentStreamsClient  Streams  { get; }
	KurrentRegistryClient Registry { get; }
	KurrentFeaturesClient Features { get; }
}
