using System.Diagnostics;

namespace Kurrent.Client;

/// <summary>
/// Provides functionality to resolve and retrieve types by their full names,
/// searching the currently loaded assemblies or scanned assemblies.
/// </summary>
static class SystemTypes {
    public static readonly Type MissingType = Type.Missing.GetType();

    public static bool IsBytes(this object value) => value.GetType().IsBytes();

    public static bool IsBytes(this Type type) =>
        type == typeof(byte[]) ||
        type == typeof(ReadOnlyMemory<byte>) ||
        type == typeof(Memory<byte>);

    /// <summary>
    /// Checks if the specified type is the placeholder type representing a missing type.
    /// </summary>
    /// <param name="source">
    /// The <see cref="Type"/> to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the type is the placeholder for a missing type; otherwise, <c>false</c>.
    /// </returns>
    [DebuggerStepThrough]
    public static bool IsMissing(this Type source) => source == MissingType;

    /// <summary>
    /// Resolves and retrieves a <see cref="Type"/> by its fully qualified name.
    /// Searches the currently loaded assemblies or scanned assemblies if the type is not found initially.
    /// Returns a special placeholder type if the specified type name cannot be resolved.
    /// </summary>
    /// <param name="fullName">The fully qualified name of the type to resolve.</param>
    /// <returns>The <see cref="Type"/> object matching the specified full name, or a placeholder type if not found.</returns>
    public static Type ResolveType(string fullName) =>
        Type.GetType(fullName) ?? AssemblyScanner.System.Scan()
            .InstancesWithFullName(fullName)
            .FirstOrMissing();

    public static Type ResolveTypeOrThrow(string fullName) =>
        Type.GetType(fullName)
     ?? AssemblyScanner.System.Scan().InstancesWithFullName(fullName).FirstOrDefault()
     ?? throw new InvalidOperationException($"Type '{fullName}' could not be resolved. Ensure the type is defined in a loaded assembly or scanned assemblies.");

    /// <summary>
    /// Attempts to resolve and retrieve a <see cref="Type"/> by its fully qualified name.
    /// Searches the currently loaded assemblies or scanned assemblies.
    /// Returns a boolean indicating whether the type was successfully resolved.
    /// </summary>
    /// <param name="fullName">The fully qualified name of the type to resolve.</param>
    /// <param name="type">When this method returns, contains the resolved <see cref="Type"/> if the resolution was successful; otherwise, contains a placeholder type.</param>
    /// <returns><see langword="true"/> if the type was successfully resolved; otherwise, <see langword="false"/>.</returns>
    public static bool TryResolveType(string fullName, out Type type) => (type = ResolveType(fullName)) != MissingType;

    /// <summary>
    /// Determines whether the specified type is an instantiable class.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns><c>true</c> if the type is a non-abstract class; otherwise, <c>false</c>.</returns>
    public static bool IsInstantiableClass(this Type type) => type is { IsClass: true, IsAbstract: false };

    /// <summary>
    /// Determines whether the type's full name matches the specified name.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="name">The full name to compare against.</param>
    /// <returns><c>true</c> if the type's full name matches the specified name; otherwise, <c>false</c>.</returns>
    public static bool MatchesFullName(this Type type, string name) => type.FullName?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Determines whether the namespace of the specified <see cref="Type"/> matches the given namespace prefix.
    /// </summary>
    /// <param name="type">
    /// The <see cref="Type"/> whose namespace is to be compared.
    /// </param>
    /// <param name="namespacePrefix">
    /// The namespace prefix to compare with the namespace of the specified type.
    /// </param>
    /// <returns>
    /// <c>true</c> if the namespace of the specified type matches the namespace prefix; otherwise, <c>false</c>.
    /// </returns>
    public static bool MatchesNamespace(this Type type, string namespacePrefix) =>
        type.Namespace?.Equals(namespacePrefix, StringComparison.OrdinalIgnoreCase) ?? false;
}
