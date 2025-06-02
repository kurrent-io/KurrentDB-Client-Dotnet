using Grpc.Core;

namespace KurrentDB.Client.Tests.Next;

public class KurrentDBGossipResolverTests {



	// Helper method to extract Uri from channel for verification
	Uri GetUriFromChannel(ChannelBase channel) =>
		// This is a simplification - in a real implementation,
		// you would need to get the target from the channel
		// For testing, you might need to create a wrapper or use channel options
		new($"https://{channel}");
}
