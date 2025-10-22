using System.Collections.Immutable;
using Humanizer;
using Microsoft.Extensions.Configuration;

namespace KurrentDB.Client.Tests;

public static class GlobalEnvironment {
	public static double MaxAppendSize => 2.Megabytes().Bytes;

	static GlobalEnvironment() {
		EnsureDefaults(Application.Configuration);

		UseCluster        = Application.Configuration.GetValue<bool>("ES_USE_CLUSTER");
		UseExternalServer = Application.Configuration.GetValue<bool>("ES_USE_EXTERNAL_SERVER");
		DockerImage       = Application.Configuration.GetValue<string>("TESTCONTAINER_KURRENTDB_IMAGE")!;

		Variables = Application.Configuration.AsEnumerable()
			.Where(x => x.Key.StartsWith("TESTCONTAINER_") || x.Key.StartsWith("EVENTSTORE_") || x.Key.StartsWith("KURRENTDB_"))
			.OrderBy(x => x.Key)
			.ToImmutableDictionary(x => x.Key, x => x.Value ?? string.Empty)!;

		return;

		static void EnsureDefaults(IConfiguration configuration) {
			// internal defaults
			configuration.EnsureValue("TESTCONTAINER_KURRENTDB_IMAGE", "docker.cloudsmith.io/eventstore/kurrent-latest/kurrentdb:latest");

			// database defaults
			configuration.EnsureValue("EVENTSTORE_TELEMETRY_OPTOUT", "true");
			configuration.EnsureValue("EVENTSTORE_ALLOW_UNKNOWN_OPTIONS", "true");
			configuration.EnsureValue("EVENTSTORE_RUN_PROJECTIONS", "None");
			configuration.EnsureValue("EVENTSTORE_START_STANDARD_PROJECTIONS", "false");
			configuration.EnsureValue("EVENTSTORE_MEM_DB", "true");
			configuration.EnsureValue("EVENTSTORE_CERTIFICATE_FILE", "/etc/eventstore/certs/node/node.crt");
			configuration.EnsureValue("EVENTSTORE_CERTIFICATE_PRIVATE_KEY_FILE", "/etc/eventstore/certs/node/node.key");
			configuration.EnsureValue("EVENTSTORE_TRUSTED_ROOT_CERTIFICATES_PATH", "/etc/eventstore/certs/ca");
			configuration.EnsureValue("EVENTSTORE__PLUGINS__USERCERTIFICATES__ENABLED", "true");
			configuration.EnsureValue("EVENTSTORE_STREAM_EXISTENCE_FILTER_SIZE", "10000");
			configuration.EnsureValue("EVENTSTORE_STREAM_INFO_CACHE_CAPACITY", "10000");
			configuration.EnsureValue("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true");
			configuration.EnsureValue("EVENTSTORE_LOG_LEVEL", "Default");
			configuration.EnsureValue("EVENTSTORE_DISABLE_LOG_FILE", "true");
			configuration.EnsureValue("EVENTSTORE_MAX_APPEND_SIZE", $"{MaxAppendSize}");
			configuration.EnsureValue("EVENTSTORE_MAX_APPEND_EVENT_SIZE", $"{MaxAppendSize}");

		}
	}

	public static ImmutableDictionary<string, string?> Variables { get; }

	public static bool   UseCluster        { get; }
	public static bool   UseExternalServer { get; }
	public static string DockerImage       { get; }
}
