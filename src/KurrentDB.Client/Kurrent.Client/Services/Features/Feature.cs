using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Features;

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