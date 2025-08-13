using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using Grpc.Net.Client;
using KurrentDB.Client;

namespace Kurrent.Client.Legacy;

/// <summary>
/// A gRPC CallInvoker implementation that delegates to the LegacyClusterClient to obtain
/// channel information. This provides a bridge between the legacy gossip-based discovery
/// and modern gRPC client patterns.
/// </summary>
/// <remarks>
/// This implementation ensures that ServerCapabilities are always up-to-date and accessible
/// in a thread-safe manner. It transparently handles getting the correct channel invoker
/// from the LegacyClusterClient for each gRPC call.
/// </remarks>
sealed class KurrentDBLegacyCallInvoker : CallInvoker, IAsyncDisposable {
	readonly SemaphoreSlim       _stateLock = new(1, 1);
	readonly LegacyClusterClient _legacyClient;
	readonly bool                _disposeClient;

	volatile ServerCapabilities _currentCapabilities;
    volatile GrpcChannelOptions _currentChannelOptions;

    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KurrentDBLegacyCallInvoker"/> class.
    /// </summary>
    /// <param name="legacyClient">
    /// The legacy cluster client used to obtain channel information.
    /// </param>
    /// <param name="disposeClient">
    /// Optional; indicates whether the legacy client should be disposed when this invoker is disposed.
    /// </param>
    internal KurrentDBLegacyCallInvoker(LegacyClusterClient legacyClient, bool disposeClient = true) {
	    _legacyClient          = legacyClient;
	    _disposeClient         = disposeClient;
	    _currentCapabilities   = null!;
        _currentChannelOptions = null!;
    }

    /// <summary>
    /// Gets the current server capabilities from the connected node.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when server capabilities are not yet initialized.</exception>
    public ServerCapabilities ServerCapabilities {
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
        get {
            if (_currentCapabilities is not null)
                return _currentCapabilities;

            WithChannelInfoInvoker(CancellationToken.None);

            return _currentCapabilities ?? throw new InvalidOperationException(
                "Ensure the client is connected before accessing server capabilities.");
        }
    }

    /// <summary>
    /// Gets the current channel options used for gRPC calls.
    /// This includes the HTTP client and other options necessary for making gRPC calls.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when server capabilities are not yet initialized.</exception>
    public GrpcChannelOptions ChannelOptions {
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
        get {
            if (_currentChannelOptions is not null)
                return _currentChannelOptions;

            WithChannelInfoInvoker(CancellationToken.None);

            return _currentChannelOptions ?? throw new InvalidOperationException(
                "Ensure the client is connected before accessing channel options.");
        }
    }

    /// <summary>
    /// Forces a refresh of the internal channel info and server capabilities by attempting to reconnect to the cluster.
    /// </summary>
    public async Task ForceRefresh(CancellationToken cancellationToken) {
        ObjectDisposedException.ThrowIf(_disposed, this);

	    // must be called for every gRPC operation as per requirements
	    var channelInfo = await _legacyClient.ForceReconnect().ConfigureAwait(false);

	    // update capabilities in a thread-safe manner
		await UpdateChannelInfo(channelInfo, cancellationToken);
    }

    /// <summary>
    /// Gets a channel info invoker safely without deadlock risks.
    /// <remarks>
    /// Asynchronously gets the current <see cref="CallInvoker"/> from the legacy cluster client.
    /// Updates the server capabilities as a side effect.
    /// </remarks>
    /// </summary>
    CallInvoker WithChannelInfoInvoker(CancellationToken cancellationToken) {
	    ObjectDisposedException.ThrowIf(_disposed, this);

        // using Task.Run to avoid deadlocks in synchronous contexts,
	    // this is necessary because the legacy client may block on network operations,
	    // and we want to ensure we don't block the calling thread.
	    return Task.Run(
	        () => GetInvokerAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
	        cancellationToken
        ).GetAwaiter().GetResult();

	    async Task<CallInvoker> GetInvokerAsync() {
		    // must be called for every gRPC operation as per requirements
		    var channelInfo = await _legacyClient.Connect(cancellationToken).ConfigureAwait(false);

		    // update capabilities and channel target in a thread-safe manner
		    await UpdateChannelInfo(channelInfo, cancellationToken);

		    return channelInfo.CallInvoker;
	    }
    }

    /// <summary>
    ///  Updates the server capabilities and other properties in a thread-safe manner.
    /// </summary>
    async Task UpdateChannelInfo(ChannelInfo channelInfo, CancellationToken cancellationToken = default) {
        var options      = channelInfo.Options;
        var capabilities = channelInfo.ServerCapabilities;

	    // update capabilities in a thread-safe manner
	    await _stateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
	    try {
            _currentChannelOptions = options;
		    _currentCapabilities   = capabilities;
	    } finally {
		    _stateLock.Release();
	    }
    }

    #region . CallInvoker Overrides .

    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
	    WithChannelInfoInvoker(options.CancellationToken).AsyncUnaryCall(method, host, options, request);

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
	    WithChannelInfoInvoker(options.CancellationToken).BlockingUnaryCall(method, host, options, request);

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) =>
	    WithChannelInfoInvoker(options.CancellationToken).AsyncClientStreamingCall(method, host, options);

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
	    WithChannelInfoInvoker(options.CancellationToken).AsyncServerStreamingCall(method, host, options, request);

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) =>
	    WithChannelInfoInvoker(options.CancellationToken).AsyncDuplexStreamingCall(method, host, options);

    #endregion

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() {
	    if (_disposed)
		    return;

	    _disposed = true;
	    _stateLock.Dispose();

	    if (_disposeClient)
		    await _legacyClient.DisposeAsync().ConfigureAwait(false);
    }
}
