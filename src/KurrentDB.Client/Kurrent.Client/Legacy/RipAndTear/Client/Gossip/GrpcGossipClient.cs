using System.Net;
using EventStore.Client;
using EventStore.Client.Gossip;
using Grpc.Core;

namespace KurrentDB.Client;

class GrpcGossipClient(KurrentDBClientSettings settings) : IGossipClient {
    public async ValueTask<ClusterMessages.ClusterInfo> GetAsync(ChannelBase channel, CancellationToken ct) {
        var client = new Gossip.GossipClient(channel);

        using var call = client.ReadAsync(new Empty(), KurrentDBCallOptions.CreateNonStreaming(settings, ct));

        var result = await call.ResponseAsync.ConfigureAwait(false);

        return new(
            result.Members
                .Select(x =>
                    new ClusterMessages.MemberInfo(
                        Uuid.FromDto(x.InstanceId),
                        (ClusterMessages.VNodeState)x.State,
                        x.IsAlive,
                        new DnsEndPoint(x.HttpEndPoint.Address, (int)x.HttpEndPoint.Port)
                    )
                ).ToArray()
        );
    }
}
