// using System.Text.Encodings.Web;
// using System.Threading.Channels;
// using Grpc.Core;
// using Microsoft.Extensions.Logging;
//
// namespace Kurrent.Client;
//
// /// <summary>
// /// The client used to manage persistent subscriptions in the KurrentDB.
// /// </summary>
// public sealed partial class KurrentDBPersistentSubscriptionsClient : KurrentDBClientBase {
// 	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new (capacity: 1) {
// 		SingleReader                  = true,
// 		SingleWriter                  = true,
// 		AllowSynchronousContinuations = true
// 	};
//
// 	readonly ILogger            _log;
// 	readonly Lazy<HttpFallback> _httpFallback;
//
// 	/// <summary>
// 	/// Constructs a new <see cref="KurrentDBPersistentSubscriptionsClient"/>.
// 	/// </summary>
// 	public KurrentDBPersistentSubscriptionsClient(KurrentDBClientSettings? settings) : base(settings,
// 		new Dictionary<string, Func<RpcException, Exception>> {
// 			[Constants.Exceptions.PersistentSubscriptionDoesNotExist] = ex => new
// 				PersistentSubscriptionNotFoundException(
// 					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
// 					ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.GroupName)?.Value ?? "", ex),
// 			[Constants.Exceptions.MaximumSubscribersReached] = ex => new
// 				MaximumSubscribersReachedException(
// 					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
// 					ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex),
// 			[Constants.Exceptions.PersistentSubscriptionDropped] = ex => new
// 				PersistentSubscriptionDroppedByServerException(
// 					ex.Trailers.First(x => x.Key == Constants.Exceptions.StreamName).Value,
// 					ex.Trailers.First(x => x.Key == Constants.Exceptions.GroupName).Value, ex)
// 		}) {
// 		_log = Settings.LoggerFactory.CreateLogger<KurrentDBPersistentSubscriptionsClient>();
//
// 		_httpFallback = new Lazy<HttpFallback>(() => new HttpFallback(Settings));
// 	}
//
// 	/// Returns the result of an HTTP Get request based on the client settings.
// 	Task<T> HttpGet<T>(
// 		string path, Action onNotFound, ChannelInfo channelInfo,
// 		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
// 	) => _httpFallback.Value.HttpGetAsync<T>(path, channelInfo, deadline, userCredentials, onNotFound, cancellationToken);
//
// 	/// Executes an HTTP Post request based on the client settings.
// 	Task HttpPost(
// 		string path, string query, Action onNotFound, ChannelInfo channelInfo,
// 		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
// 	) => _httpFallback.Value.HttpPostAsync(path, query, channelInfo, deadline, userCredentials, onNotFound, cancellationToken);
//
// 	protected override ValueTask DisposeAsyncCore() {
// 		if (_httpFallback.IsValueCreated)
// 			_httpFallback.Value.Dispose();
//
// 		return ValueTask.CompletedTask;
// 	}
//
// 	static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
// }

// ReSharper disable InconsistentNaming

using System.Text.Encodings.Web;
using System.Threading.Channels;
using EventStore.Client.PersistentSubscriptions;
using Grpc.Core;
using Kurrent.Client.Legacy;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using StreamMetadata = Kurrent.Client.Model.StreamMetadata;

namespace Kurrent.Client;

public sealed partial class KurrentDBPersistentSubscriptionsClient {
	internal KurrentDBPersistentSubscriptionsClient(CallInvoker callInvoker, KurrentClientOptions options) {
		Options = options;

		options.Mapper.Map<StreamMetadata>("$metadata");

		Registry          = new KurrentRegistryClient(callInvoker);
		LegacySettings    = options.ConvertToLegacySettings();
		LegacyCallInvoker = new KurrentDBLegacyCallInvoker(new LegacyClusterClient(LegacySettings));
		ServiceClient     = new PersistentSubscriptions.PersistentSubscriptionsClient(callInvoker);

		_log = LegacySettings.LoggerFactory.CreateLogger<KurrentDBPersistentSubscriptionsClient>();

		_httpFallback = new Lazy<HttpFallback>(() => new HttpFallback(LegacySettings));
	}

	internal KurrentClientOptions                                  Options       { get; }
	internal KurrentRegistryClient                                 Registry      { get; }
	internal PersistentSubscriptions.PersistentSubscriptionsClient ServiceClient { get; }

	internal KurrentDBLegacyCallInvoker LegacyCallInvoker { get; }
	internal KurrentDBClientSettings    LegacySettings    { get; }

	readonly ILogger            _log;
	readonly Lazy<HttpFallback> _httpFallback;

	static readonly BoundedChannelOptions ReadBoundedChannelOptions = new(capacity: 1) {
		SingleReader                  = true,
		SingleWriter                  = true,
		AllowSynchronousContinuations = true
	};

	/// Returns the result of an HTTP Get request based on the client settings.
	Task<T> HttpGet<T>(
		string path, Action onNotFound, ChannelInfo channelInfo,
		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
	) =>
		_httpFallback.Value.HttpGetAsync<T>(
			path, channelInfo, deadline,
			userCredentials, onNotFound, cancellationToken
		);

	/// Executes an HTTP Post request based on the client settings.
	Task HttpPost(
		string path, string query, Action onNotFound, ChannelInfo channelInfo,
		TimeSpan? deadline, UserCredentials? userCredentials, CancellationToken cancellationToken
	) =>
		_httpFallback.Value.HttpPostAsync(
			path, query, channelInfo,
			deadline, userCredentials, onNotFound,
			cancellationToken
		);

	static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
