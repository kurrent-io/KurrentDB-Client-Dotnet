// ReSharper disable InconsistentNaming

using System.Text.Encodings.Web;
using System.Threading.Channels;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.SchemaRegistry;
using Kurrent.Client.SchemaRegistry.Serialization;
using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
using Kurrent.Client.SchemaRegistry.Serialization.Json;
using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
using KurrentDB.Client;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public sealed partial class KurrentPersistentSubscriptionsClient {
	readonly Lazy<HttpFallback> HttpFallback;

	internal KurrentPersistentSubscriptionsClient(CallInvoker callInvoker, KurrentClientOptions options) {
		Options = options;

		options.Mapper.Map<StreamMetadata>("$metadata");

		Registry = new KurrentRegistryClient(callInvoker);

		var schemaExporter = new SchemaExporter();
		var schemaManager  = new SchemaManager(Registry, schemaExporter, options.Mapper);

		SerializerProvider = new SchemaSerializerProvider(
			[
				new BytesPassthroughSerializer(),
				new JsonSchemaSerializer(
					new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
					schemaManager
				),
				new ProtobufSchemaSerializer(
					new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
					schemaManager
				)
			]
		);

		LegacySettings    = options.ConvertToLegacySettings();
		LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(LegacySettings, ExceptionMap, dontLoadServerCapabilities: false));
		ServiceClient     = new PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker);
		LegacyClient      = new KurrentDBPersistentSubscriptionsClient(LegacySettings);
		HttpFallback      = new Lazy<HttpFallback>(() => new HttpFallback(LegacySettings));

		LegacyConverter = new KurrentDBLegacyConverter(
			SerializerProvider,
			options.MetadataDecoder,
			SchemaRegistryPolicy.NoRequirements
		);
	}

	internal KurrentClientOptions                                  Options            { get; }
	internal KurrentRegistryClient                                 Registry           { get; }
	internal ISchemaSerializerProvider                             SerializerProvider { get; }
	internal PersistentSubscriptions.PersistentSubscriptionsClient ServiceClient      { get; }

	internal KurrentDBLegacyCallInvoker             LegacyCallInvoker { get; }
	internal KurrentDBClientSettings                LegacySettings    { get; }
	internal KurrentDBLegacyConverter               LegacyConverter   { get; }
	internal KurrentDBPersistentSubscriptionsClient LegacyClient      { get; }

	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(capacity: 1) {
		SingleReader                  = true,
		SingleWriter                  = true,
		AllowSynchronousContinuations = true
	};

	Task<T> HttpGet<T>(string path, Action onNotFound, CancellationToken cancellationToken) =>
		HttpFallback.Value.HttpGetAsync<T>(
			path, LegacyCallInvoker.ChannelTarget, deadline: null,
			userCredentials: null, onNotFound, cancellationToken
		);

	Task HttpPost(string path, string query, Action onNotFound, CancellationToken cancellationToken) =>
		HttpFallback.Value.HttpPostAsync(
			path, query, LegacyCallInvoker.ChannelTarget,
			deadline: null, userCredentials: null, onNotFound,
			cancellationToken
		);

	static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);

	static Dictionary<string, Func<RpcException, Exception>> ExceptionMap =>
		new() {
			[Constants.Exceptions.PersistentSubscriptionDoesNotExist] = ex => new
				PersistentSubscriptionNotFoundException(
					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
					ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value ?? "", ex
				),
			[Constants.Exceptions.MaximumSubscribersReached] = ex => new
				MaximumSubscribersReachedException(
					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
					ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex
				),
			[Constants.Exceptions.PersistentSubscriptionDropped] = ex => new
				PersistentSubscriptionDroppedByServerException(
					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
					ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex
				)
		};
}
