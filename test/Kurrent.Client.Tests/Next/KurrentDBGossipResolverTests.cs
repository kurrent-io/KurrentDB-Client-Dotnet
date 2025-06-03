using Grpc.Core;

namespace Kurrent.Client.Tests.Next;

public class KurrentDBGossipResolverTests {
	// Helper method to extract Uri from channel for verification
	Uri GetUriFromChannel(ChannelBase channel) => new($"https://{channel}");
}
