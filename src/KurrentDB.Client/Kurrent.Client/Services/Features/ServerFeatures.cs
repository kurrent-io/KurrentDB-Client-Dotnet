using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Features;

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