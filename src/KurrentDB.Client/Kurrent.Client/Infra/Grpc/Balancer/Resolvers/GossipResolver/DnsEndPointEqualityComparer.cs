using System.Net;

namespace Kurrent.Grpc.Balancer;

public class DnsEndPointEqualityComparer : IEqualityComparer<DnsEndPoint> {
	public static readonly DnsEndPointEqualityComparer Default = new();

	/// <summary>
	/// Determines whether the specified endpoints are equal.
	/// </summary>
	/// <param name="x">The first endpoint.</param>
	/// <param name="y">The second endpoint.</param>
	/// <returns>True if equal, otherwise false.</returns>
	public bool Equals(DnsEndPoint? x, DnsEndPoint? y) {
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;

		return string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase) && x.Port == y.Port;
	}

	/// <summary>
	/// Gets the hash code for the endpoint.
	/// </summary>
	/// <param name="obj">The endpoint.</param>
	/// <returns>The hash code.</returns>
	public int GetHashCode(DnsEndPoint obj) =>
		HashCode.Combine(obj.Host.ToLowerInvariant(), obj.Port);
}
