using Grpc.Net.Client.Balancer;

namespace Kurrent.Grpc.Balancer;

public delegate PickResult PickBalancerChannel(IReadOnlyList<Subchannel> subchannels, PickContext context);
/// <summary>
/// A generic subchannel picker that uses a function to generate pick results.
/// </summary>
public class GenericChannelPicker(IReadOnlyList<Subchannel> subchannels, PickBalancerChannel pick) : SubchannelPicker {
	public static SubchannelPicker Create(IReadOnlyList<Subchannel> subchannels, PickBalancerChannel pick) =>
		new GenericChannelPicker(subchannels, pick);

	public override PickResult Pick(PickContext context) => pick(subchannels, context);
}
