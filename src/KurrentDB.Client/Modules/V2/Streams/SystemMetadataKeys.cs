using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
public static class SystemMetadataKeys {
    const string SystemPrefix = "$";

    public static bool IsSystemMetadataKey(string key) =>
	    key.StartsWith(SystemPrefix, StringComparison.Ordinal);

    public const string SchemaName       = $"{SystemPrefix}schema.name";
    public const string SchemaDataFormat = $"{SystemPrefix}schema.data-format";
    public const string SchemaVersionId  = $"{SystemPrefix}schema.version-id";
    public const string SchemaUrn        = $"{SystemPrefix}schema.urn"; // NEW represents the schema name + format + version id

    #region internal

    /// <summary>
    /// possibly used internally and then removed right after.
    /// </summary>
    public const string Stream = $"{SystemPrefix}stream.name";

    /// <summary>
    /// not sure yet, but will be required when using the new contracts because
    /// old metadata will be sent inside the new Record properties
    /// </summary>
    public const string HasProperties = $"{SystemPrefix}has-properties";

    #endregion
}
