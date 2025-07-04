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
using Microsoft.Extensions.Logging;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public sealed partial class KurrentPersistentSubscriptionsClient {
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
		LegacyConverter   = new KurrentDBLegacyConverter(SerializerProvider, options.MetadataDecoder, SchemaRegistryPolicy.NoRequirements);

		HttpFallback = new Lazy<HttpFallback>(() => new HttpFallback(LegacySettings));
		Logger          = LegacySettings.LoggerFactory.CreateLogger<KurrentDBPersistentSubscriptionsClient>();
	}

	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(capacity: 1) {
		SingleReader                  = true,
		SingleWriter                  = true,
		AllowSynchronousContinuations = true
	};

	internal KurrentClientOptions                                  Options            { get; }
	internal KurrentRegistryClient                                 Registry           { get; }
	internal KurrentDBLegacyCallInvoker                            LegacyCallInvoker  { get; }
	internal KurrentDBClientSettings                               LegacySettings     { get; }
	internal KurrentDBLegacyConverter                              LegacyConverter    { get; }
	internal ISchemaSerializerProvider                             SerializerProvider { get; }
	internal PersistentSubscriptions.PersistentSubscriptionsClient ServiceClient      { get; }

	readonly Lazy<HttpFallback> HttpFallback;
	readonly ILogger            Logger;

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

	async Task EnsureCompatibility(string streamName, CancellationToken cancellationToken) {
		if (streamName is not SystemStreams.AllStream) return;

		await LegacyCallInvoker.ForceRefresh(cancellationToken);

		if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsToAll)
			throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
	}

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
}
