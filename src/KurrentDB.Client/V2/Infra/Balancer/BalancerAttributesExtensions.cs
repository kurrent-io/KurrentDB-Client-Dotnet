using Grpc.Net.Client.Balancer;

namespace Kurrent.Grpc.Balancer;

public static class BalancerAttributesExtensions {
	public static BalancerAttributes WithValue<T>(this BalancerAttributes attributes, string key, T value) {
		if (string.IsNullOrWhiteSpace(key))
			throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));

		attributes.Set(new BalancerAttributesKey<T>(key), value);

		return attributes;
	}

	public static T GetValueOrDefault<T>(this BalancerAttributes attributes,  string key, T defaultValue) =>
		attributes.TryGetValue(new BalancerAttributesKey<T>(key), out var value) && value is not null ?  value : defaultValue;

	public static T GetValue<T>(this BalancerAttributes attributes,  string key) =>
		attributes.TryGetValue(new BalancerAttributesKey<T>(key), out var value) && value is not null ?  value :
			throw new KeyNotFoundException($"Key '{key}' not found in BalancerAttributes or value is null.");
}
