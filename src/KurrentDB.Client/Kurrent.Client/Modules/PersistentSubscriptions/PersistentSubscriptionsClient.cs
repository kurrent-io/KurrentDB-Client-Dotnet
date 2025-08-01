// ReSharper disable InconsistentNaming

using System.Text.Encodings.Web;
using EventStore.Client.PersistentSubscriptions;
using Kurrent.Client.Legacy;
using Kurrent.Client.Registry;
using Kurrent.Client.Schema.Serialization;
using Kurrent.Client.Streams;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using static EventStore.Client.PersistentSubscriptions.PersistentSubscriptions;

namespace Kurrent.Client;

public sealed partial class PersistentSubscriptionsClient {
	internal PersistentSubscriptionsClient(KurrentClient source) {
		Registry           = source.Registry;
		SerializerProvider = source.SerializerProvider;

		MetadataDecoder = source.MetadataDecoder;

		LegacySettings    = source.Options.ConvertToLegacySettings();
		LegacyCallInvoker = source.LegacyCallInvoker;
		ServiceClient     = new PersistentSubscriptions.PersistentSubscriptionsClient(source.LegacyCallInvoker);

		HttpFallback = new Lazy<HttpFallback>(() => new HttpFallback(LegacySettings));
		Logger       = source.Options.LoggerFactory.CreateLogger<KurrentDBPersistentSubscriptionsClient>();
	}

	internal RegistryClient                                        Registry           { get; }
	internal KurrentDBLegacyCallInvoker                            LegacyCallInvoker  { get; }
	internal KurrentDBClientSettings                               LegacySettings     { get; }
	internal ISchemaSerializerProvider                             SerializerProvider { get; }
	internal IMetadataDecoder                                      MetadataDecoder    { get; }
	internal PersistentSubscriptions.PersistentSubscriptionsClient ServiceClient      { get; }

	readonly Lazy<HttpFallback> HttpFallback;
	readonly ILogger            Logger;

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
