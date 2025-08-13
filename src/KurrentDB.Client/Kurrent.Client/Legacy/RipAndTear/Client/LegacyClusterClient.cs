#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Kurrent.Client;
using Kurrent.Grpc.Interceptors;
using KurrentDB.Client.Interceptors;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client;

/// <summary>
/// Just an attempt to make the components a bit more usable and readable until we can
/// get rid of the legacy code related with .NET48 and custom gRPC channels and discovery.
/// </summary>
class LegacyClusterClient {
    readonly CancellationTokenSource                            _cancellator;
    readonly ChannelCache                                       _channelCache;
    readonly SharingProvider<ReconnectionRequired, ChannelInfo> _channelInfoProvider;

    bool _disposed;

    public LegacyClusterClient(
        KurrentDBClientSettings settings, Dictionary<string, Func<RpcException, Exception>> exceptionMap, bool dontLoadServerCapabilities = false
    ) {
        _cancellator  = new CancellationTokenSource();
        _channelCache = new(settings);

        var clientName    = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
        var clientVersion = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";

        // TODO SS: this should become node read preference.
        var requiresLeader = settings.ConnectivitySettings.NodePreference == NodePreference.Leader ? bool.TrueString : bool.FalseString;

        IChannelSelector channelSelector = settings.ConnectivitySettings.IsSingleNode
            ? new SingleNodeChannelSelector(settings, _channelCache)
            : new GossipChannelSelector(settings, _channelCache, new GrpcGossipClient(settings));

        var token = _cancellator.Token;

        var logger = settings.LoggerFactory.CreateLogger<SharingProvider>();

        _channelInfoProvider = new SharingProvider<ReconnectionRequired, ChannelInfo>(
            async (reconnectionRequired, _) => {
                var channelInfo = reconnectionRequired switch {
                    ReconnectionRequired.Rediscover               => await channelSelector.SelectChannelAsync(token).ConfigureAwait(false),
                    ReconnectionRequired.NewLeader (var endpoint) => channelSelector.SelectEndpointChannel(endpoint)
                };

                var invoker = channelInfo.Channel.CreateCallInvoker().Intercept(ConfigureInterceptors());

                if (dontLoadServerCapabilities)
                    return new(channelInfo.Channel, channelInfo.Options, new ServerCapabilities(), invoker);

                ServerCapabilities capabilities = new();

                try {
                    capabilities = await new GrpcServerCapabilitiesClient(settings)
                        .GetAsync(invoker, token)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) {
                    logger.LogWarning(ex, "Failed to get server capabilities. This may lead to unexpected behavior.");
                }

                return new(channelInfo.Channel, channelInfo.Options, capabilities, invoker);
            },
            settings.ConnectivitySettings.DiscoveryInterval,
            ReconnectionRequired.Rediscover.Instance,
            null,
            settings.LoggerFactory.CreateLogger($"SharingProvider-{settings.ConnectionName}")
        );

        ResolverScheme = settings.ConnectivitySettings.IsSingleNode ? "kurrentdb" : "kurrentdb+discover";

        return;

        Interceptor[] ConfigureInterceptors() {
            var requiresLeaderInterceptor = HeadersInterceptor.InjectHeaders(
                Constants.Headers.RequiresLeader, requiresLeader,
                "MultiStreamAppendSession"
            );

            var headersInterceptor = HeadersInterceptor.InjectHeaders(
                new() {
                    [Constants.Headers.ClientName]     = clientName,
                    [Constants.Headers.ClientVersion]  = clientVersion,
                    [Constants.Headers.ConnectionName] = settings.ConnectionName!
                }
            );

            var topologyChangesInterceptor = new LeaderNotFoundInterceptor(
                this, settings.LoggerFactory.CreateLogger<LeaderNotFoundInterceptor>()
            );

            var authenticationInterceptor = new AuthenticationInterceptor(settings);

            return exceptionMap.Count == 0
                ? [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, topologyChangesInterceptor, authenticationInterceptor]
                : [
                    ..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, new TypedExceptionInterceptor(exceptionMap),
                    topologyChangesInterceptor, authenticationInterceptor
                ];

            // return exceptionMap.Count == 0
            // 	? [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, topologyChangesInterceptor ]
            // 	: [..settings.Interceptors, requiresLeaderInterceptor, headersInterceptor, new TypedExceptionInterceptor(exceptionMap), topologyChangesInterceptor ];
        }
    }

    public string ResolverScheme { get; }

    public static LegacyClusterClient Create(KurrentDBClientSettings settings) => new(settings, [], true);

    // public static LegacyClusterClient CreateWithExceptionMapping(KurrentDBClientSettings settings) => new(settings, ExceptionMap, true);

    public async ValueTask<ChannelInfo> Connect(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        return await _channelInfoProvider.CurrentAsync.WithCancellation(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ChannelInfo> ForceReconnect(DnsEndPoint? leaderEndpoint = null) {
        ThrowIfDisposed();

        ReconnectionRequired reconnectionRequired = leaderEndpoint is not null
            ? new ReconnectionRequired.NewLeader(leaderEndpoint)
            : ReconnectionRequired.Rediscover.Instance;

        _channelInfoProvider.Reset(reconnectionRequired);

        return await _channelInfoProvider.CurrentAsync.ConfigureAwait(false);
    }

    internal void TriggerReconnect(DnsEndPoint? leaderEndpoint = null) {
        ThrowIfDisposed();
        _channelInfoProvider.Reset(
            leaderEndpoint is not null
                ? new ReconnectionRequired.NewLeader(leaderEndpoint)
                : ReconnectionRequired.Rediscover.Instance
        );
    }

    public async ValueTask DisposeAsync() {
        if (_disposed)
            return;

        _disposed = true;
        _channelInfoProvider.Dispose();

        await _cancellator.CancelAsync();
        _cancellator.Dispose();

        await _channelCache
            .DisposeAsync()
            .ConfigureAwait(false);
    }

    void ThrowIfDisposed() {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LegacyClusterClient), "The legacy cluster client has already been disposed and cannot be used anymore.");
    }
}

/// <summary>
/// Exception thrown when an append exceeds the maximum size set by the server.
/// </summary>
class MaximumAppendSizeExceededException : Exception {
    /// <summary>
    /// Constructs a new <see cref="MaximumAppendSizeExceededException"/>.
    /// </summary>
    /// <param name="maxAppendSize"></param>
    /// <param name="innerException"></param>
    public MaximumAppendSizeExceededException(uint maxAppendSize, Exception? innerException = null) :
        base($"Maximum Append Size of {maxAppendSize} Exceeded.", innerException) =>
        MaxAppendSize = maxAppendSize;

    /// <summary>
    /// Constructs a new <see cref="MaximumAppendSizeExceededException"/>.
    /// </summary>
    /// <param name="maxAppendSize"></param>
    /// <param name="innerException"></param>
    public MaximumAppendSizeExceededException(int maxAppendSize, Exception? innerException = null) : this((uint)maxAppendSize, innerException) { }

    /// <summary>
    /// The configured maximum append size.
    /// </summary>
    public uint MaxAppendSize { get; }
}

// /// <summary>
// /// Exception thrown if there is an attempt to operate inside a
// /// transaction which does not exist.
// /// </summary>
// class InvalidTransactionException : Exception {
//     /// <summary>
//     /// Constructs a new <see cref="InvalidTransactionException"/>.
//     /// </summary>
//     public InvalidTransactionException() { }
//
//     /// <summary>
//     /// Constructs a new <see cref="InvalidTransactionException"/>.
//     /// </summary>
//     public InvalidTransactionException(string message) : base(message) { }
//
//     /// <summary>
//     /// Constructs a new <see cref="InvalidTransactionException"/>.
//     /// </summary>
//     public InvalidTransactionException(string message, Exception innerException) : base(message, innerException) { }
//
//     /// <summary>
//     /// Constructs a new <see cref="InvalidTransactionException"/>.
//     /// </summary>
//     [Obsolete("Obsolete")]
//     protected InvalidTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
// }
