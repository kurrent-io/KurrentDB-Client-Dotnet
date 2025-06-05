using Grpc.Core;

namespace KurrentDB.Client;

/// <summary>
/// A gRPC CallInvoker implementation that delegates to the LegacyClusterClient to obtain
/// channel information. This provides a bridge between the legacy gossip-based discovery
/// and modern gRPC client patterns.
/// </summary>
/// <remarks>
/// This implementation ensures that ServerCapabilities are always up-to-date and accessible
/// in a thread-safe manner. It transparently handles obtaining the correct channel invoker
/// from the LegacyClusterClient for each gRPC call.
/// </remarks>
public sealed class KurrentDBLegacyCallInvoker : CallInvoker, IDisposable {
    readonly SemaphoreSlim       _capabilitiesLock = new(1, 1);
    readonly LegacyClusterClient _legacyClient;
    volatile ServerCapabilities  _currentCapabilities;
    bool                         _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KurrentDBLegacyCallInvoker"/> class.
    /// </summary>
    /// <param name="legacyClient">The legacy cluster client used to obtain channel information.</param>
    internal KurrentDBLegacyCallInvoker(LegacyClusterClient legacyClient) {
	    _legacyClient = legacyClient;
	    _currentCapabilities = null!;
    }

    /// <summary>
    /// Gets the current server capabilities from the connected node.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when server capabilities are not yet initialized.</exception>
    public ServerCapabilities ServerCapabilities => _currentCapabilities ?? throw new InvalidOperationException(
        "Server capabilities are not initialized. Ensure the client is connected before accessing this property."
    );

    /// <summary>
    /// Gets an invoker safely without deadlock risks.
    /// <remarks>
    /// Asynchronously gets the current <see cref="CallInvoker"/> from the legacy cluster client.
    /// Updates the server capabilities as a side effect.
    /// </remarks>
    /// </summary>
    CallInvoker GetLegacyInvoker(CancellationToken cancellationToken) {
	    if (_disposed)
		    throw new ObjectDisposedException(nameof(KurrentDBLegacyCallInvoker));

        return Task.Run(
	        () => GetInvokerAsync(cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult(),
	        cancellationToken
        ).GetAwaiter().GetResult();

        async Task<CallInvoker> GetInvokerAsync(CancellationToken cancellationToken) {
	        // must be called for every gRPC operation as per requirements
	        var channelInfo = await _legacyClient.Connect(cancellationToken).ConfigureAwait(false);

	        // update capabilities in a thread-safe manner
	        await _capabilitiesLock.WaitAsync(cancellationToken).ConfigureAwait(false);
	        try {
		        _currentCapabilities = channelInfo.ServerCapabilities;
	        }
	        finally {
		        _capabilitiesLock.Release();
	        }

	        return channelInfo.CallInvoker;
        }
    }

    /// <inheritdoc/>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) {
        var invoker = GetLegacyInvoker(options.CancellationToken);
        return invoker.AsyncUnaryCall(method, host, options, request);
    }

    /// <inheritdoc/>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) {
        var invoker = GetLegacyInvoker(options.CancellationToken);
        return invoker.BlockingUnaryCall(method, host, options, request);
    }

    /// <inheritdoc/>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) {
        var invoker = GetLegacyInvoker(options.CancellationToken);
        return invoker.AsyncClientStreamingCall(method, host, options);
    }

    /// <inheritdoc/>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) {
        var invoker = GetLegacyInvoker(options.CancellationToken);
        return invoker.AsyncServerStreamingCall(method, host, options, request);
    }

    /// <inheritdoc/>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(Method<TRequest, TResponse> method, string? host, CallOptions options) {
        var invoker = GetLegacyInvoker(options.CancellationToken);
        return invoker.AsyncDuplexStreamingCall(method, host, options);
    }

    /// <summary>
    /// Disposes resources used by this invoker.
    /// </summary>
    public void Dispose() {
        if (_disposed)
            return;

        _disposed = true;
        _capabilitiesLock.Dispose();
    }
}
