// using Grpc.Core;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Logging.Abstractions;
// using Polly;
// using Polly.Retry;
//
// namespace KurrentDB.Client;
//
// record struct ExecuteCallContext<TClient>(TClient ServiceClient, ServerCapabilities Capabilities, CancellationToken CancellationToken);
//
// /// <summary>
// /// Resilient client for making gRPC calls to a Kurrent cluster with automatic retries and connection caching.
// /// </summary>
// /// <typeparam name="TClient">The type of gRPC client to wrap</typeparam>
// partial class ResilientServiceClient<TClient> where TClient : ClientBase<TClient> {
// 	readonly ILegacyClusterClient         _clusterClient;
// 	readonly KurrentDBClientRetrySettings _retrySettings;
// 	readonly ILogger                      _logger;
// 	readonly ResiliencePipeline           _resiliencePipeline;
// 	readonly object                       _clientLock = new();
//
// 	// Cache the constructor info for better performance
// 	static readonly System.Reflection.ConstructorInfo ClientConstructor =
// 		typeof(TClient).GetConstructor([typeof(CallInvoker)])!;
//
// 	TClient?           _cachedClient;
// 	ServerCapabilities _serverCapabilities;
//
// 	/// <summary>
// 	/// Initializes a new instance of the ResilientKurrentClient class
// 	/// </summary>
// 	/// <param name="clusterClient">The cluster client to use for connections</param>
// 	/// <param name="retrySettings">Retry settings to use</param>
// 	/// <param name="logger">Optional logger</param>
// 	public ResilientServiceClient(ILegacyClusterClient clusterClient, KurrentDBClientRetrySettings retrySettings, ILogger? logger = null) {
// 		_clusterClient      = clusterClient;
// 		_retrySettings      = retrySettings;
// 		_logger             = logger ?? NullLogger<ResilientServiceClient<TClient>>.Instance;
// 		_resiliencePipeline = CreateResiliencePipeline();
//
// 		_serverCapabilities = new ServerCapabilities();
// 	}
//
// 	/// <summary>
// 	/// Gets whether the client is currently connected
// 	/// </summary>
// 	public bool IsConnected { get; private set; }
//
// 	/// <summary>
// 	/// Executes a gRPC call with automatic retry and failover, reusing the client when possible
// 	/// </summary>
// 	public ValueTask<TResponse> Execute<TResponse>(
// 		Func<ExecuteCallContext<TClient>, ValueTask<TResponse>> handler,
// 		CancellationToken cancellationToken = default) {
// 		return _resiliencePipeline.ExecuteAsync(async innerToken => {
// 			var client = await GetOrCreateClientAsync(innerToken).ConfigureAwait(false);
// 			return await handler(new(client, _serverCapabilities, innerToken)).ConfigureAwait(false);
// 		}, cancellationToken);
// 	}
//
// 	/// <summary>
// 	/// Gets the cached client if available or creates a new one
// 	/// </summary>
// 	async Task<TClient> GetOrCreateClientAsync(CancellationToken cancellationToken) {
// 		// Fast path: check if we already have a cached client
// 		if (IsConnected && _cachedClient is not null)
// 			return _cachedClient;
//
// 		// Need to create a new client with synchronization
// 		lock (_clientLock) {
// 			// Double-check inside the lock
// 			if (IsConnected && _cachedClient is not null)
// 				return _cachedClient;
//
// 			// Mark as not connected while we're connecting
// 			IsConnected = false;
// 		}
//
// 		try {
// 			LogConnecting();
//
// 			// Connect to the cluster to get a call invoker and server capabilities
// 			var (callInvoker, capabilities) = await _clusterClient
// 				.Connect(cancellationToken)
// 				.ConfigureAwait(false);
//
// 			// Create a new client
// 			var newClient = (TClient)ClientConstructor.Invoke([callInvoker]);
//
// 			// Store the new client and capabilities
// 			lock (_clientLock) {
// 				_cachedClient       = newClient;
// 				_serverCapabilities = capabilities;
// 				IsConnected         = true;
// 			}
//
// 			LogConnected();
//
// 			return newClient;
// 		}
// 		catch (Exception ex) {
// 			LogConnectionFailed(ex);
// 			throw;
// 		}
// 	}
//
// 	/// <summary>
// 	/// Creates a Polly resilience pipeline based on the retry settings
// 	/// </summary>
// 	ResiliencePipeline CreateResiliencePipeline() {
// 		if (!_retrySettings.IsEnabled) {
// 			LogRetryDisabled();
// 			return new ResiliencePipelineBuilder().Build();
// 		}
//
// 		LogCreatingRetryPipeline();
//
// 		var builder = new ResiliencePipelineBuilder();
//
// 		builder.AddRetry(new RetryStrategyOptions {
// 			MaxRetryAttempts = _retrySettings.MaxAttempts - 1, // -1 because first attempt is not a retry
//
// 			BackoffType = _retrySettings.BackoffMultiplier > 1
// 				? DelayBackoffType.Exponential
// 				: DelayBackoffType.Constant,
//
// 			UseJitter = true,
// 			Delay     = _retrySettings.InitialBackoff,
// 			MaxDelay  = _retrySettings.MaxBackoff,
//
// 			ShouldHandle = new PredicateBuilder()
// 				.Handle<Exception>(ex => ex?.InnerException is NotLeaderException)
// 				.Handle<RpcException>(ex => IsRetryableStatusCode(ex.StatusCode)),
//
// 			OnRetry = async retryContext => {
// 				if (retryContext.Outcome.Exception is RpcException rpcEx)
// 					LogRetrying(retryContext.AttemptNumber, _retrySettings.MaxAttempts, rpcEx.StatusCode);
// 				else
// 					LogRetrying(retryContext.AttemptNumber, _retrySettings.MaxAttempts, null);
//
// 				// Invalidate client and refresh cluster info before retrying
// 				lock (_clientLock) {
// 					LogInvalidatingClient();
// 					_cachedClient = null;
// 					IsConnected   = false;
// 					// We don't clear ServerCapabilities because users might still need to reference it
// 				}
//
// 				// If we have a new leader, we need to update the cluster client
// 				if (retryContext.Outcome.Exception?.InnerException is NotLeaderException notLeaderEx)
// 					await _clusterClient.ForceReconnect(notLeaderEx.LeaderEndpoint);
// 				else
// 					await _clusterClient.ForceReconnect();
// 			}
// 		});
//
// 		return builder.Build();
//
// 		bool IsRetryableStatusCode(StatusCode statusCode) =>
// 			_retrySettings.RetryableStatusCodes.Any(retryableCode => statusCode == retryableCode);
// 	}
//
// 	#region Logging
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Connecting to KurrentDB")]
// 	partial void LogConnecting();
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Successfully connected to KurrentDB")]
// 	partial void LogConnected();
//
// 	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to connect to KurrentDB")]
// 	partial void LogConnectionFailed(Exception ex);
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Invalidating KurrentDB client connection")]
// 	partial void LogInvalidatingClient();
//
// 	[LoggerMessage(Level = LogLevel.Information, Message = "Creating resilience pipeline with retry settings")]
// 	partial void LogCreatingRetryPipeline();
//
// 	[LoggerMessage(Level = LogLevel.Information, Message = "Retry is disabled in settings")]
// 	partial void LogRetryDisabled();
//
// 	[LoggerMessage(Level = LogLevel.Debug, Message = "Retry {AttemptNumber}/{MaxAttempts} for RPC call. Status: {StatusCode}")]
// 	partial void LogRetrying(int attemptNumber, int maxAttempts, StatusCode? statusCode);
//
// 	#endregion
// }
