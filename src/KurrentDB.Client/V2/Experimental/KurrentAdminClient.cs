// using Grpc.Core;
// using Kurrent.Client.Features;
// using Kurrent.Client.Model;
// using static EventStore.Client.ServerFeatures.ServerFeatures;
//
// namespace KurrentDB.Client.ServerInfo;
//
// [PublicAPI]
// public class KurrentAdminClient {
// 	// Custom empty request to avoid using the default one from the library.... sigh... -_-'
// 	static readonly EventStore.Client.Empty CustomEmptyRequest = new();
//
// 	internal KurrentAdminClient(CallInvoker invoker) =>
// 		ServiceClient = new ServerFeaturesClient(invoker);
//
// 	ServerFeaturesClient ServiceClient { get; }
//
// 	/// <summary>
// 	/// Gets server information including features and their enablement status.
// 	/// </summary>
// 	/// <param name="ct">
// 	/// Cancellation token to cancel the request if needed.
// 	/// </param>
// 	/// <returns>Server information with features.</returns>
// 	public async Task<Kurrent.Client.Features.ServerInfo> GetServerInfo(CancellationToken ct = default) {
// 		// Get the raw methods and their features from the server
// 		var response = await ServiceClient
// 			.GetSupportedMethodsAsync(CustomEmptyRequest, cancellationToken: ct)
// 			.ConfigureAwait(false);
//
// 		// Create a dictionary of raw features by method name for efficient lookups
// 		var rawMethods = response.Methods.Aggregate(
// 			new Dictionary<string, HashSet<string>>(),
// 			(seed, method) => {
// 				seed.Add($"{method.ServiceName}.{method.MethodName}", method.Features.ToHashSet());
// 				return seed;
// 			}
// 		);
//
// 		// Extract high-level features from raw methods
// 		var features = new List<Feature>();
//
// 		ExtractStreamsFeatures(rawMethods, features);
// 		ExtractPersistentSubscriptionFeatures(rawMethods, features);
// 		ExtractSchemaRegistryFeatures(rawMethods, features);
//
// 		// In the future, we can expand this to include other server information
// 		return new Kurrent.Client.Features.ServerInfo {
// 			Version  = response.EventStoreServerVersion,
// 			Features = new ServerFeatures(features),
// 		};
// 	}
//
// 	static void ExtractSchemaRegistryFeatures(Dictionary<string, HashSet<string>> rawMethods, List<Feature> features) {
// 		// Base prefix for schema registry service
// 		var servicePrefix = "event_store.client.schema_registry";
// 		var methodPrefix  = $"{servicePrefix}.schemaregistry";
//
// 		// Check if schema registry is enabled by looking for any methods with this service
// 		var exists = rawMethods.Keys.Any(k => k.StartsWith(methodPrefix));
//
// 		if (!exists)
// 			return;
//
// 		// Extract feature requirements
// 		var requirements = new List<FeatureRequirement> {
// 			// Basic capability flags
// 			new() {
// 				Name        = "RegistrationSupport",
// 				Value       = rawMethods.TryGetValue($"{methodPrefix}.register", out _),
// 				Description = "Schema registry registration support"
// 			},
// 			new() {
// 				Name        = "ValidationSupport",
// 				Value       = rawMethods.TryGetValue($"{methodPrefix}.validate", out _),
// 				Description = "Schema registry validation support"
// 			}
// 		};
//
// 		// Parse registration requirement (with enum typing)
// 		if (rawMethods.SelectMany(kvp => kvp.Value).FirstOrDefault(f => f.StartsWith($"{servicePrefix}.registration:")) is { } regFlag) {
// 			var value = regFlag.Split(':')[1].ToLowerInvariant();
//
// 			var enforcementLevel = value switch {
// 				"required"   => EnforcementLevel.Required,
// 				"prohibited" => EnforcementLevel.Prohibited,
// 				_            => EnforcementLevel.Optional
// 			};
//
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name             = "RegistrationRequirement",
// 					Value            = enforcementLevel,
// 					EnforcementLevel = enforcementLevel,
// 					Description      = "Schema registration requirement level"
// 				}
// 			);
// 		}
// 		else {
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name             = "RegistrationRequirement",
// 					Value            = EnforcementLevel.Optional,
// 					EnforcementLevel = EnforcementLevel.Optional,
// 					Description      = "Schema registration requirement level"
// 				}
// 			);
// 		}
//
// 		// Parse validation requirement (with enum typing)
// 		if (rawMethods.SelectMany(kvp => kvp.Value).FirstOrDefault(f => f.StartsWith($"{servicePrefix}.validation:")) is { } valFlag) {
// 			var value = valFlag.Split(':')[1].ToLowerInvariant();
// 			var policyStatus = value switch {
// 				"required"   => EnforcementLevel.Required,
// 				"prohibited" => EnforcementLevel.Prohibited,
// 				_            => EnforcementLevel.Optional
// 			};
//
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name             = "ValidationRequirement",
// 					Value            = policyStatus,
// 					EnforcementLevel = policyStatus,
// 					Description      = "Schema validation requirement level"
// 				}
// 			);
// 		}
// 		else {
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name             = "ValidationRequirement",
// 					Value            = EnforcementLevel.Optional,
// 					EnforcementLevel = EnforcementLevel.Optional,
// 					Description      = "Schema validation requirement level"
// 				}
// 			);
// 		}
//
// 		// Parse data format (with enum typing)
// 		if (rawMethods.SelectMany(kvp => kvp.Value).FirstOrDefault(f => f.StartsWith($"{servicePrefix}.format:")) is { } formatFlag) {
// 			var value = formatFlag.Split(':')[1].ToLowerInvariant();
// 			var dataFormat = value switch {
// 				"json"     => SchemaDataFormat.Json,
// 				"avro"     => SchemaDataFormat.Avro,
// 				"protobuf" => SchemaDataFormat.Protobuf,
// 				_          => SchemaDataFormat.Unspecified
// 			};
//
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name        = "EnforcedDataFormat",
// 					Value       = dataFormat,
// 					Description = "Required schema data format"
// 				}
// 			);
// 		}
// 		else {
// 			requirements.Add(
// 				new FeatureRequirement {
// 					Name        = "EnforcedDataFormat",
// 					Value       = SchemaDataFormat.Unspecified,
// 					Description = "Required schema data format"
// 				}
// 			);
// 		}
//
// 		// Create the feature
// 		features.Add(
// 			new Feature {
// 				Name         = "SchemaRegistry",
// 				Description  = "Supports schema registration and validation",
// 				Enabled      = true,
// 				Requirements = requirements
// 			}
// 		);
// 	}
//
// 	static void ExtractPersistentSubscriptionFeatures(Dictionary<string, HashSet<string>> rawMethods, List<Feature> features) {
// 		var prefix = "event_store.client.persistent_subscriptions.persistentsubscriptions";
// 		var psFeatures = new Dictionary<string, bool> {
// 			["SubscribeToAll"]   = rawMethods.TryGetValue($"{prefix}.read", out var f) && f.Contains("all"),
// 			["GetInfo"]          = rawMethods.ContainsKey($"{prefix}.getinfo"),
// 			["RestartSubsystem"] = rawMethods.ContainsKey($"{prefix}.restartsubsystem"),
// 			["ReplayParked"]     = rawMethods.ContainsKey($"{prefix}.replayparked"),
// 			["List"]             = rawMethods.ContainsKey($"{prefix}.list")
// 		};
//
// 		if (psFeatures.Values.Any(v => v)) {
// 			var requirements = psFeatures.Select(kv => new FeatureRequirement {
// 					Name             = kv.Key,
// 					Value            = kv.Value,
// 					EnforcementLevel = EnforcementLevel.Optional,
// 					Description      = $"Supports {kv.Key} for persistent subscriptions"
// 				}
// 			).ToList();
//
// 			features.Add(
// 				new Feature {
// 					Name         = "PersistentSubscriptions",
// 					Description  = "Supports persistent subscriptions",
// 					Enabled      = true,
// 					Requirements = requirements
// 				}
// 			);
// 		}
// 	}
//
// 	static void ExtractStreamsFeatures(Dictionary<string, HashSet<string>> rawMethods, List<Feature> features) {
// 		if (rawMethods.ContainsKey("event_store.client.streams.streams.batchappend"))
// 			features.Add(
// 				new Feature {
// 					Name        = "BatchAppend",
// 					Description = "Supports appending multiple events in a single transaction",
// 					Enabled     = true,
// 					Deprecated  = true
// 				}
// 			);
// 	}
// }
