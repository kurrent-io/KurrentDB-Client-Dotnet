using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using Kurrent.Client.Legacy;

namespace KurrentDB.Client;

// Maintains Channels keyed by DnsEndPoint so the channels can be reused.
// Deals with the disposal difference between grpc.net and grpc.core
// Thread safe.
class ChannelCache(KurrentDBClientSettings settings) : IAsyncDisposable {
    readonly Dictionary<DnsEndPoint, (GrpcChannel Channel, GrpcChannelOptions Options)> _channels = new(DnsEndPointEqualityComparer.Instance);

    readonly object _lock   = new();
    readonly Random _random = new(0);
    bool            _disposed;

    public async ValueTask DisposeAsync() {
        GrpcChannel[] channelsToDispose;

        lock (_lock) {
            if (_disposed)
                return;

            _disposed = true;

            channelsToDispose = _channels.Values.Select(x => x.Channel).ToArray();

            _channels.Clear();
        }

        await DisposeChannelsAsync(channelsToDispose).ConfigureAwait(false);
    }

    public (GrpcChannel Channel, GrpcChannelOptions Options) CreateChannel(DnsEndPoint endPoint) {
        lock (_lock) {
            ThrowIfDisposed();

            if (_channels.TryGetValue(endPoint, out var channelInfo))
                return channelInfo;

            var channel = settings.CreateChannel(endPoint, out var options);

            return _channels[endPoint] = (channel, options);
        }
    }

    public KeyValuePair<DnsEndPoint, GrpcChannel>[] GetRandomOrderSnapshot() {
        lock (_lock) {
            ThrowIfDisposed();

            return _channels
                .OrderBy(_ => _random.Next())
                .Select(kvp => new KeyValuePair<DnsEndPoint, GrpcChannel>(kvp.Key, kvp.Value.Channel))
                .ToArray();
        }
    }

    // Update the cache to contain channels for exactly these endpoints
    public void UpdateCache(DnsEndPoint[] endPoints) {
        lock (_lock) {
            ThrowIfDisposed();

            // Create a HashSet for efficient lookups
            var endPointSet       = new HashSet<DnsEndPoint>(endPoints, DnsEndPointEqualityComparer.Instance);
            var channelsToDispose = new List<GrpcChannel>();

            // Remove entries not in the new set (single pass)
            foreach (var kvp in _channels.Where(kvp => !endPointSet.Contains(kvp.Key))) {
                _channels.Remove(kvp.Key);
                channelsToDispose.Add(kvp.Value.Channel);
            }

            // Dispose removed channels
            if (channelsToDispose.Count > 0)
                _ = DisposeChannelsAsync(channelsToDispose);

            // Add new endpoints (avoiding duplication)
            foreach (var endPoint in endPoints)
                if (!_channels.ContainsKey(endPoint))
                    CreateChannel(endPoint);
        }
    }

    void ThrowIfDisposed() {
        lock (_lock) {
            if (_disposed)
                throw new ObjectDisposedException(GetType().ToString());
        }
    }

    static Task DisposeChannelsAsync(IEnumerable<GrpcChannel> channels) {
        return Task.WhenAll(channels.Select(channel => DisposeAsync(channel).AsTask()));

        static async ValueTask DisposeAsync(ChannelBase channel) {
            await channel.ShutdownAsync().ConfigureAwait(false);
            (channel as IDisposable)?.Dispose();
        }
    }

    class DnsEndPointEqualityComparer : IEqualityComparer<DnsEndPoint> {
        public static readonly DnsEndPointEqualityComparer Instance = new();

        public bool Equals(DnsEndPoint? x, DnsEndPoint? y) {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null)
                return false;

            if (y is null)
                return false;

            if (x.GetType() != y.GetType())
                return false;

            return
                string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase) &&
                x.Port == y.Port;
        }

        public int GetHashCode(DnsEndPoint obj) {
            unchecked {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Host) * 397) ^
                       obj.Port;
            }
        }
    }
}
