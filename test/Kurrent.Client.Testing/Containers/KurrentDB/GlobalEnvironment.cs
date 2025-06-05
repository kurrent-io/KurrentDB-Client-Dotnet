// using System.Collections.Immutable;
// using Microsoft.Extensions.Configuration;
// using static System.StringComparison;
//
// using EnvVars = System.Collections.Immutable.ImmutableDictionary<string, string>;
//
// namespace Kurrent.Client.Testing.Containers.KurrentDB;
//
// public static class GlobalEnvironment {
// 	static GlobalEnvironment() {
// 		EnsureDefaults(ApplicationContext.Configuration);
//
// 		UseCluster        = ApplicationContext.Configuration.GetValue<bool>("DEVEX_CI_USE_CLUSTER");
// 		UseExternalServer = ApplicationContext.Configuration.GetValue<bool>("DEVEX_CI_USE_EXTERNAL_SERVER");
//
// 		Variables = ExtractEnvironmentVariables(ApplicationContext.Configuration);
//
// 		return;
//
// 		static void EnsureDefaults(IConfiguration configuration) {
// 			configuration.EnsureValue("DEVEX_CI_USE_CLUSTER", "false");
// 			configuration.EnsureValue("DEVEX_CI_USE_EXTERNAL_SERVER", "false");
// 			configuration.EnsureValue("DEVEX_CI_DOCKER_REGISTRY", "docker.kurrent.io/kurrent-latest/kurrentdb");  // latest
// 			configuration.EnsureValue("DEVEX_CI_DOCKER_TAG", "latest");
// 			configuration.EnsureValue("DEVEX_CI_DOCKER_IMAGE", $"{configuration["ES_DOCKER_REGISTRY"]}:{configuration["ES_DOCKER_TAG"]}");
//
// 			configuration.EnsureValue("TESTCONTAINER_KURRENTDB_IMAGE", "docker.kurrent.io/kurrent-latest/kurrentdb:latest");
//
// 			configuration.EnsureValue("KURRENTDB_TELEMETRY_OPTOUT", "true");
// 			configuration.EnsureValue("KURRENTDB_ALLOW_UNKNOWN_OPTIONS", "true");
// 			configuration.EnsureValue("KURRENTDB_MEM_DB", "false");
//
// 			configuration.EnsureValue("KURRENTDB_RUN_PROJECTIONS", "None");
// 			configuration.EnsureValue("KURRENTDB_START_STANDARD_PROJECTIONS", "false");
//
// 			configuration.EnsureValue("KURRENTDB_LOG_LEVEL", "Default"); // required to use serilog settings
// 			configuration.EnsureValue("KURRENTDB_DISABLE_LOG_FILE", "true");
//
// 			configuration.EnsureValue("KURRENTDB__PLUGINS__USERCERTIFICATES__ENABLED", "true");
// 			configuration.EnsureValue("KURRENTDB_CERTIFICATE_FILE", "/etc/kurrentdb/certs/node/node.crt");
// 			configuration.EnsureValue("KURRENTDB_CERTIFICATE_PRIVATE_KEY_FILE", "/etc/kurrentdb/certs/node/node.key");
// 			configuration.EnsureValue("KURRENTDB_TRUSTED_ROOT_CERTIFICATES_PATH", "/etc/kurrentdb/certs/ca");
//
// 			configuration.EnsureValue("KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP", "true");
// 		}
//
// 		static EnvVars ExtractEnvironmentVariables(IConfiguration configuration) {
// 			return configuration.AsEnumerable()
// 				.Select(x => x switch {
// 					_ when x.Key.StartsWith("ES_", OrdinalIgnoreCase)            => (Key: $"KURRENTDB_{x.Key[3..]}", Value: x.Value ?? string.Empty),
// 					_ when x.Key.StartsWith("EVENTSTORE_", OrdinalIgnoreCase)    => (Key: $"KURRENTDB_{x.Key[12..]}", Value: x.Value ?? string.Empty),
// 					_ when x.Key.StartsWith("KURRENTDB_", OrdinalIgnoreCase)     => (Key: x.Key, Value: x.Value ?? string.Empty),
// 					_ when x.Key.StartsWith("TESTCONTAINER_", OrdinalIgnoreCase) => (Key: x.Key, Value: x.Value ?? string.Empty),
// 					_ when x.Key.StartsWith("DEVEX_CI_", OrdinalIgnoreCase)      => (Key: x.Key, Value: x.Value ?? string.Empty),
// 					_                                                            => default
// 				})
// 				.Where(x => x != default)
// 				.OrderBy(x => x.Key)
// 				.ToImmutableDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
// 		}
// 	}
//
// 	public static EnvVars Variables         { get; }
// 	public static bool    UseCluster        { get; }
// 	public static bool    UseExternalServer { get; }
// }
//
// static class ConfigurationExtensions {
// 	public static void EnsureValue(this IConfiguration configuration, string key, string defaultValue) {
// 		var value = configuration.GetValue<string?>(key);
// 		if (string.IsNullOrEmpty(value))
// 			configuration[key] = defaultValue;
// 	}
// }


