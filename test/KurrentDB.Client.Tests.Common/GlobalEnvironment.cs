﻿using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;

namespace KurrentDB.Client.Tests;

public static class GlobalEnvironment {
	static GlobalEnvironment() {
		EnsureDefaults(Application.Configuration);

		UseCluster        = Application.Configuration.GetValue<bool>("ES_USE_CLUSTER");
		UseExternalServer = Application.Configuration.GetValue<bool>("ES_USE_EXTERNAL_SERVER");
		DockerImage       = Application.Configuration.GetValue<string>("ES_DOCKER_IMAGE")!;

		Variables = Application.Configuration.AsEnumerable()
			.Where(x => x.Key.StartsWith("ES_") || x.Key.StartsWith("EVENTSTORE_") || x.Key.StartsWith("KURRENTDB_"))
			.OrderBy(x => x.Key)
			.ToImmutableDictionary(x => x.Key, x => x.Value ?? string.Empty)!;

		return;

		static void EnsureDefaults(IConfiguration configuration) {
			configuration.EnsureValue("ES_USE_CLUSTER", "false");
			configuration.EnsureValue("ES_USE_EXTERNAL_SERVER", "false");

			// configuration.EnsureValue("ES_DOCKER_REGISTRY", "docker.kurrent.io/kurrent-preview/kurrentdb");
			// configuration.EnsureValue("ES_DOCKER_TAG", "25.0.1-experimental-arm64-8.0-jammy");

			configuration.EnsureValue("ES_DOCKER_REGISTRY", "docker.cloudsmith.io/eventstore/kurrent-staging/kurrentdb");
			configuration.EnsureValue("ES_DOCKER_TAG", "ci");

			configuration.EnsureValue("ES_DOCKER_IMAGE", $"{configuration["ES_DOCKER_REGISTRY"]}:{configuration["ES_DOCKER_TAG"]}");

			configuration.EnsureValue("KURRENTDB_TELEMETRY_OPTOUT", "true");
			configuration.EnsureValue("KURRENTDB_ALLOW_UNKNOWN_OPTIONS", "true");
			configuration.EnsureValue("KURRENTDB_MEM_DB", "false");
			configuration.EnsureValue("KURRENTDB_RUN_PROJECTIONS", "None");
			configuration.EnsureValue("KURRENTDB_START_STANDARD_PROJECTIONS", "false");
			configuration.EnsureValue("KURRENTDB_LOG_LEVEL", "Information");
			configuration.EnsureValue("KURRENTDB_DISABLE_LOG_FILE", "true");
			configuration.EnsureValue("KURRENTDB_TRUSTED_ROOT_CERTIFICATES_PATH", "/etc/kurrentdb/certs/ca");
			configuration.EnsureValue("KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP", "true");
			// configuration.EnsureValue("KURRENTDB_APPLICATION_MAX_APPEND_SIZE", "4194304");
		}
	}

	public static ImmutableDictionary<string, string?> Variables { get; }

	public static bool   UseCluster        { get; }
	public static bool   UseExternalServer { get; }
	public static string DockerImage       { get; }
}
