using System.Reflection;

namespace Kurrent.Client;

/// <summary>
/// Provides detailed version and attribute information for an assembly.
/// </summary>
class AppVersionInfo {
    public static readonly AppVersionInfo Current = GetCurrent();

    /// <summary>
    /// Gets the title of the assembly.
    /// From <see cref="AssemblyTitleAttribute"/>.
    /// </summary>
    public string? Title { get; private init; }

    /// <summary>
    /// Gets the description of the assembly.
    /// From <see cref="AssemblyDescriptionAttribute"/>.
    /// </summary>
    public string? Description { get; private init; }

    /// <summary>
    /// Gets the product name of the assembly.
    /// From <see cref="AssemblyProductAttribute"/>.
    /// </summary>
    public string? ProductName { get; private init; }

    /// <summary>
    /// Gets the company name.
    /// From <see cref="AssemblyCompanyAttribute"/>.
    /// </summary>
    public string? CompanyName { get; private init; }

    /// <summary>
    /// Gets the copyright information.
    /// From <see cref="AssemblyCopyrightAttribute"/>.
    /// </summary>
    public string? Copyright { get; private init; }

    /// <summary>
    /// Gets the trademark information.
    /// From <see cref="AssemblyTrademarkAttribute"/>.
    /// </summary>
    public string? Trademark { get; private init; }

    /// <summary>
    /// Gets the product version string, typically the marketing version (e.g., "1.0.0-beta").
    /// From <see cref="AssemblyInformationalVersionAttribute"/>.
    /// </summary>
    public string? ProductVersion { get; private init; }

    /// <summary>
    /// Gets the file version string (e.g., "1.0.0.42").
    /// From <see cref="AssemblyFileVersionAttribute"/>.
    /// </summary>
    public string? FileVersion { get; private init; }

    /// <summary>
    /// Gets the assembly's strong name version as a <see cref="System.Version"/> object.
    /// From <see cref="AssemblyName.Version"/>.
    /// </summary>
    public Version? AssemblyVersion { get; private init; }

    /// <summary>
    /// Retrieves version information for the entry assembly of the current application.
    /// </summary>
    /// <returns>
    /// An <see cref="AppVersionInfo"/> object populated with details from the entry assembly.
    /// If the entry assembly can't be determined (e.g., in some hosting scenarios or unit tests),
    /// an <see cref="AppVersionInfo"/> object with all properties set to null is returned.
    /// </returns>
    static AppVersionInfo GetCurrent() {
        var entryAssembly = Assembly.GetExecutingAssembly();
        return GetForAssembly(entryAssembly);
    }

    /// <summary>
    /// Retrieves version information for the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to retrieve information from.</param>
    /// <returns>An <see cref="AppVersionInfo"/> object populated with details from the specified assembly.</returns>
    static AppVersionInfo GetForAssembly(Assembly assembly) =>
        new() {
            Title           = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title,
            Description     = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description,
            ProductName     = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product,
            CompanyName     = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company,
            Copyright       = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
            Trademark       = assembly.GetCustomAttribute<AssemblyTrademarkAttribute>()?.Trademark,
            ProductVersion  = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            FileVersion     = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
            AssemblyVersion = assembly.GetName().Version
        };
}
