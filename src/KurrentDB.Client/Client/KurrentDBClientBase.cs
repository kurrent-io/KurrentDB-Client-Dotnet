using Grpc.Core;
using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry;
using KurrentDB.Client.SchemaRegistry.Serialization;
using KurrentDB.Client.SchemaRegistry.Serialization.Bytes;
using KurrentDB.Client.SchemaRegistry.Serialization.Json;
using KurrentDB.Client.SchemaRegistry.Serialization.Protobuf;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

namespace KurrentDB.Client;

/// <summary>
/// The base class used by clients used to communicate with the KurrentDB.
/// </summary>
public abstract class KurrentDBClientBase : IAsyncDisposable {

	/// Constructs a new <see cref="KurrentDBClientBase"/>.
	protected KurrentDBClientBase(KurrentDBClientSettings? settings, Dictionary<string, Func<RpcException, Exception>>? exceptionMap = null) {
		Settings = settings ?? new();

		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";

		LegacyClusterClient = new(Settings, exceptionMap ?? new());

		var typeMapper     = new MessageTypeMapper();
		var schemaExporter = new SchemaExporter();
		var registryClient = new KurrentRegistryClient(Settings);

		var schemaManager = new SchemaManager(registryClient, schemaExporter, typeMapper);

		SerializerProvider = new SchemaSerializerProvider([
			new BytesPassthroughSerializer(), // How to enforce registry policies for this serializer?
			new JsonSchemaSerializer(
				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
				schemaManager: schemaManager
			),
			new ProtobufSchemaSerializer(
				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
				schemaManager: schemaManager
			)
		]);

		MetadataDecoder = Settings.MetadataDecoder;

		DataConverter = new LegacyDataConverter(
			SerializerProvider,
			Settings.MetadataDecoder,
			SchemaRegistryPolicy.NoRequirements
		);

	}

	internal KurrentDBClientSettings Settings { get; }

	LegacyClusterClient LegacyClusterClient { get;  }

	protected internal ISchemaSerializerProvider SerializerProvider { get; }
	protected internal IMetadataDecoder          MetadataDecoder    { get; }
	protected internal LegacyDataConverter       DataConverter      { get; }

	#region . Legacy Stuff .

	// [Obsolete("You should not use this property, use the Service Clients properties available on this class instead.", false)]
	// internal ChannelInfo ChannelInfo { get; private set; } = null!;

	// [Obsolete("Stop using this method and use the Service Clients properties available on this class instead.", false)]
	protected internal ValueTask<ChannelInfo> GetChannelInfo(CancellationToken cancellationToken = default) =>
		LegacyClusterClient.Connect(cancellationToken);

	protected internal async ValueTask<ChannelInfo> RediscoverAsync() =>
		await LegacyClusterClient.ForceReconnect().ConfigureAwait(false);

	#endregion

	protected virtual ValueTask DisposeAsyncCore() => new();

	public async ValueTask DisposeAsync() {
		await DisposeAsyncCore();

		await LegacyClusterClient
			.DisposeAsync()
			.ConfigureAwait(false);

		GC.SuppressFinalize(this);
	}

	internal async ValueTask<ServiceClientConnection<T>> Connect<T>(CancellationToken cancellationToken) where T : class {
		var (_, capabilities, invoker) = await GetChannelInfo(cancellationToken).ConfigureAwait(false);
		var serviceClient = (T)Activator.CreateInstance(typeof(T), invoker)!;
		return new ServiceClientConnection<T>(serviceClient, capabilities);
	}
}

record ServiceClientConnection<T>(T Client, ServerCapabilities Capabilities);
