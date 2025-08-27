using OpenTelemetry.Trace;

namespace Kurrent.Client.Extensions;

/// <summary>
/// Provides extension methods for configuring OpenTelemetry tracing for the KurrentDB .NET Client.
/// </summary>
public static class TracerProviderBuilderExtensions {
    /// <summary>
    /// Adds KurrentDB .NET Client instrumentation to the <see cref="TracerProviderBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="TracerProviderBuilder"/> to configure.</param>
    /// <returns>The configured <see cref="TracerProviderBuilder"/>.</returns>
    public static TracerProviderBuilder AddKurrentClientInstrumentation(this TracerProviderBuilder builder) =>
        builder.AddSource(AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client");
}
