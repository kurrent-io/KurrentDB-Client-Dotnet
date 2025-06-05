using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Features;

// /// <summary>
// /// Represents server information including metadata and available features.
// /// </summary>
// public record ServerInfo {
// 	// /// <summary>
// 	// /// Unique identifier for this server node.
// 	// /// </summary>
// 	// public string NodeId { get; init; } = Guid.Empty.ToString();
//
// 	/// <summary>
// 	/// Semantic version of the server.
// 	/// </summary>
// 	public string Version { get; init; } = "0.0.0";
//
// 	// /// <summary>
// 	// /// Minimum client version required to connect.
// 	// /// </summary>
// 	// public string MinCompatibleClientVersion { get; init; } = "0.0.0";
//
// 	/// <summary>
// 	/// Features available on the server, with their enablement status.
// 	/// </summary>
// 	public ServerFeatures Features { get; init; } = new();
// }

/// <summary>
/// Represents a collection of server features with their enablement status and requirements.
/// </summary>
public record ServerFeatures {
	/// <summary>
	/// Empty server features collection.
	/// </summary>
	public static readonly ServerFeatures Unknown = new();

	/// <summary>
	/// Dictionary of features by name.
	/// </summary>
	readonly Dictionary<string, Feature> _features = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Creates a new ServerFeatures instance.
	/// </summary>
	/// <param name="features">Optional collection of features to initialize with.</param>
	public ServerFeatures(IEnumerable<Feature>? features = null) {
		if (features is null)
			return;

		foreach (var feature in features)
			_features[feature.Name] = feature;
	}

	/// <summary>
	/// Semantic version of the server.
	/// </summary>
	public string Version { get; init; } = "0.0.0";

	// /// <summary>
	// /// Minimum client version required to connect.
	// /// </summary>
	// public string MinCompatibleClientVersion { get; init; } = "0.0.0";

	/// <summary>
	/// Gets all available features.
	/// </summary>
	public IReadOnlyCollection<Feature> AllFeatures =>
		_features.Values;

	/// <summary>
	/// Gets all enabled features.
	/// </summary>
	public IReadOnlyCollection<Feature> EnabledFeatures =>
		_features.Values.Where(f => f.Enabled).ToArray();

	/// <summary>
	/// Determines whether a specific feature exists and is enabled.
	/// </summary>
	/// <param name="featureName">The name of the feature to check.</param>
	/// <returns>True if the feature exists and is enabled; otherwise, false.</returns>
	public bool IsEnabled(string featureName) =>
		_features.TryGetValue(featureName, out var feature) && feature.Enabled;

	/// <summary>
	/// Determines whether a specific feature exists regardless of its enablement status.
	/// </summary>
	/// <param name="featureName">The name of the feature to check for.</param>
	/// <returns>True if the feature exists; otherwise, false.</returns>
	public bool HasFeature(string featureName) =>
		_features.ContainsKey(featureName);

	/// <summary>
	/// Retrieves a feature by its name if it exists.
	/// </summary>
	/// <param name="featureName">The name of the feature to retrieve.</param>
	/// <returns>A <see cref="Feature"/> object if the feature is found; otherwise, null.</returns>
	public Feature? GetFeature(string featureName) =>
		_features.GetValueOrDefault(featureName);

	/// <summary>
	/// Attempts to retrieve a feature by its name.
	/// </summary>
	/// <param name="featureName">The name of the feature to retrieve.</param>
	/// <param name="feature">The output parameter that will contain the requested feature if found; otherwise, null.</param>
	/// <returns>True if the feature is found; otherwise, false.</returns>
	public bool TryGetFeature(string featureName, [MaybeNullWhen(false)] out Feature feature) =>
		_features.TryGetValue(featureName, out feature);

	/// <summary>
	/// Gets all deprecated features that are still enabled.
	/// </summary>
	/// <returns>A collection of deprecated but enabled features.</returns>
	public IReadOnlyCollection<Feature> GetEnabledDeprecatedFeatures() =>
		_features.Values.Where(f => f is { Enabled: true, Deprecated: true }).ToArray();
}

/// <summary>
/// Represents a server feature with enablement status and requirements.
/// Combines aspects of both capabilities and feature flags.
/// </summary>
public record Feature {
	/// <summary>
	/// Unique name of the feature.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Human-readable description of the feature.
	/// </summary>
	public string Description { get; init; } = "";

	/// <summary>
	/// Whether this feature is enabled.
	/// </summary>
	public bool Enabled { get; init; } = true;

	/// <summary>
	/// Whether this feature can be toggled by clients.
	/// </summary>
	public bool ClientConfigurable { get; init; }

	/// <summary>
	/// Whether this feature is deprecated.
	/// </summary>
	public bool Deprecated { get; init; }

	/// <summary>
	/// For temporary features, indicates when the feature will no longer be available.
	/// </summary>
	public DateTimeOffset? AvailableUntil { get; init; }

	/// <summary>
	/// Requirements for using this feature.
	/// </summary>
	public IReadOnlyList<FeatureRequirement> Requirements { get; init; } = [];

	/// <summary>
	/// Determines whether a feature contains a specific requirement by its name.
	/// </summary>
	/// <param name="requirementName">The name of the requirement to check.</param>
	/// <returns>True if the requirement exists within the feature; otherwise, false.</returns>
	public bool HasRequirement(string requirementName) =>
		Requirements.Any(r => r.Name.Equals(requirementName, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Gets a specific requirement from the feature by its name.
	/// </summary>
	/// <param name="requirementName">The name of the requirement to retrieve.</param>
	/// <returns>The requirement matching the specified name if found; otherwise, null.</returns>
	public FeatureRequirement? GetRequirement(string requirementName) =>
		Requirements.FirstOrDefault(r => r.Name.Equals(requirementName, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Attempts to retrieve a specific requirement from the feature by its name.
	/// </summary>
	/// <param name="requirementName">The name of the requirement to retrieve.</param>
	/// <param name="requirement">When this method returns, contains the requirement matching the specified name if found; otherwise, null.</param>
	/// <returns>True if the requirement exists and was successfully retrieved; otherwise, false.</returns>
	public bool TryGetRequirement(string requirementName, [MaybeNullWhen(false)] out FeatureRequirement requirement) =>
		(requirement = GetRequirement(requirementName)) is not null;

	/// <summary>
	/// Retrieves the value of a specified requirement with an optional default value.
	/// </summary>
	/// <typeparam name="T">The expected type of the requirement value.</typeparam>
	/// <param name="requirementName">The name of the requirement whose value is to be retrieved.</param>
	/// <param name="defaultValue">The default value to return if the requirement is not found.</param>
	/// <returns>The value of the specified requirement if it exists; otherwise, the provided default value.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the requirement is not found, and no default value is provided.
	/// </exception>
	public T GetRequirementValue<T>(string requirementName, T? defaultValue = default) {
		var value = Requirements.FirstOrDefault(r => r.Name.Equals(requirementName, StringComparison.OrdinalIgnoreCase));

		if (value is not null)
			return value.GetValue<T>();

		return defaultValue ?? throw new InvalidOperationException($"Requirement '{requirementName}' not found in feature '{Name}'");
	}
}

/// <summary>
/// Represents a requirement for a feature with validation rules.
/// </summary>
public record FeatureRequirement {
	public static readonly FeatureRequirement Empty = new() {
		Name         = null!,
		Value        = false,
		EnforcementLevel = EnforcementLevel.Optional,
		Description  = null!
	};

	/// <summary>
	/// Unique name of the requirement.
	/// </summary>
	public string Name { get; init; } = "";

	/// <summary>
	/// Value of this requirement.
	/// </summary>
	public object? Value { get; init; }

	/// <summary>
	/// How this requirement is enforced.
	/// </summary>
	public EnforcementLevel EnforcementLevel { get; init; } = EnforcementLevel.Optional;

	/// <summary>
	/// Human-readable description of the requirement.
	/// </summary>
	public string Description { get; init; } = "";

	/// <summary>
	/// Message shown when requirement is violated.
	/// </summary>
	public string ViolationMessage { get; init; } = "";

	/// <summary>
	/// Converts the requirement value to the specified type.
	/// </summary>
	/// <typeparam name="T">The target type to convert to.</typeparam>
	/// <returns>The strongly-typed value.</returns>
	/// <exception cref="InvalidCastException">Thrown when the value cannot be converted to the requested type.</exception>
	/// <exception cref="FormatException">Thrown when the string representation cannot be parsed to the requested type.</exception>
	public T GetValue<T>() {
		if (Value is null)
			throw new InvalidCastException($"Cannot convert null value to {typeof(T).Name} for requirement '{Name}'");

		try {
			if (Value is T typedValue)
				return typedValue;

			if (typeof(T).IsEnum && Value is string enumStr)
				return (T)Enum.Parse(typeof(T), enumStr, true);

			if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
				return (T)Convert.ChangeType(Value, typeof(T));

			throw new InvalidCastException($"Cannot convert {Value.GetType().Name} to {typeof(T).Name} for requirement '{Name}'");
		}
		catch (Exception ex) when (ex is not InvalidCastException && ex is not FormatException) {
			throw new InvalidCastException($"Failed to convert value to {typeof(T).Name} for requirement '{Name}'", ex);
		}
	}

	/// <summary>
	/// Attempts to get the value of this requirement as a specific type.
	/// </summary>
	/// <param name="value">
	/// The output parameter that will contain the value if conversion is successful; otherwise, it will be set to default.
	/// </param>
	/// <typeparam name="T">
	/// The type to which the value should be converted. This type must be compatible with the value's type.
	/// </typeparam>
	/// <returns>
	/// True if the value was successfully converted to the specified type; otherwise, false.
	/// </returns>
	public bool TryGetValue<T>([MaybeNullWhen(false)] out T value) {
		try {
			value = GetValue<T>();
			return true;
		}
		catch (InvalidCastException) {
			value = default;
			return false;
		}
		catch (FormatException) {
			value = default;
			return false;
		}
	}
}

/// <summary>
/// The enforcement level for a feature requirement.
/// </summary>
public enum EnforcementLevel {
	/// <summary>
	/// Feature is optional with no warnings.
	/// </summary>
	Optional = 0,

	/// <summary>
	/// Feature must be enabled; operations rejected if disabled.
	/// </summary>
	Required = 1,

	/// <summary>
	/// Feature must be disabled; operations rejected if enabled.
	/// </summary>
	Prohibited = 2
}
