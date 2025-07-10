// ReSharper disable InconsistentNaming

using System.Text.Encodings.Web;
using System.Threading.Channels;
using Grpc.Core;
using Kurrent.Client.Legacy;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry.Serialization;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using static EventStore.Client.PersistentSubscriptions.PersistentSubscriptions;

namespace Kurrent.Client;

public sealed partial class KurrentPersistentSubscriptionsClient {
	internal KurrentPersistentSubscriptionsClient(KurrentClient source, KurrentClientOptions options) {
		Registry           = source.Registry;
		SerializerProvider = source.SerializerProvider;

		LegacySettings     = options.ConvertToLegacySettings();
		MetadataDecoder    = options.MetadataDecoder;

		LegacyCallInvoker  = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(LegacySettings, ExceptionMap));
		ServiceClient      = new PersistentSubscriptionsClient(LegacyCallInvoker);

		HttpFallback = new Lazy<HttpFallback>(() => new HttpFallback(LegacySettings));
		Logger          = LegacySettings.LoggerFactory.CreateLogger<KurrentDBPersistentSubscriptionsClient>();
	}

	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(capacity: 1) {
		SingleReader                  = true,
		SingleWriter                  = true,
		AllowSynchronousContinuations = true
	};

	internal KurrentRegistryClient         Registry           { get; }
	internal KurrentDBLegacyCallInvoker    LegacyCallInvoker  { get; }
	internal KurrentDBClientSettings       LegacySettings     { get; }
	internal ISchemaSerializerProvider     SerializerProvider { get; }
	internal IMetadataDecoder              MetadataDecoder    { get; }
	internal PersistentSubscriptionsClient ServiceClient      { get; }

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

	async ValueTask EnsureCompatibility(string streamName, CancellationToken cancellationToken) {
		if (streamName is not SystemStreams.AllStream) return;

		await LegacyCallInvoker.ForceRefresh(cancellationToken);

		if (!LegacyCallInvoker.ServerCapabilities.SupportsPersistentSubscriptionsToAll)
			throw new NotSupportedException("The server does not support persistent subscriptions to $all.");
	}

	Task<T> HttpGet<T>(string path, Action onNotFound, CancellationToken cancellationToken) =>
		HttpFallback.Value.HttpGetAsync<T>(
			path,
			LegacyCallInvoker.ChannelTarget,
			deadline: null,
			userCredentials: null,
			onNotFound,
			cancellationToken
		);

	Task HttpPost(string path, string query, Action onNotFound, CancellationToken cancellationToken) =>
		HttpFallback.Value.HttpPostAsync(
			path,
			query,
			LegacyCallInvoker.ChannelTarget,
			deadline: null,
			userCredentials: null,
			onNotFound,
			cancellationToken
		);

	static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
