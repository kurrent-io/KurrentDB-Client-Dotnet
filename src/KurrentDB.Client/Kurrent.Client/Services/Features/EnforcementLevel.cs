namespace Kurrent.Client.Features;

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