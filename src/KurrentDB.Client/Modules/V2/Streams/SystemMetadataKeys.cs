using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
public static class SystemMetadataKeys {
    public const string SystemPrefix = "$";

    // SchemaName and SchemaDataFormat headers are required,
    // for supporting auto SerDe even without Registry support.

    // if registry is supported, the SchemaVersionId must be sent too.

    public const string SchemaName       = $"{SystemPrefix}schema.name";         // EVENT TYPE
    public const string SchemaDataFormat = $"{SystemPrefix}schema.data-format";  // NEW
    public const string SchemaVersionId  = $"{SystemPrefix}schema.version-id";   // NEW
}
