using Microsoft.Extensions.Configuration;

namespace Kurrent.Client.Testing;

public static class ConfigurationSettingsExtensions {
	public static IConfiguration ToConfiguration(this IDictionary<string, string?> settings) =>
		new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

	public static IDictionary<string, string?> ToSettings(this IConfiguration configuration) =>
		new Dictionary<string, string?>(configuration.AsEnumerable().Where(x => x.Value is not null));
}
