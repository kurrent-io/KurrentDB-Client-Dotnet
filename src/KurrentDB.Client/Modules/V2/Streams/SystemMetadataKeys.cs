using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
public static class SystemMetadataKeys {
    public const string SystemPrefix = "$";

    // SchemaName and SchemaDataFormat headers are required,
    // for supporting auto SerDe even without Registry support.

    // if registry is supported, the SchemaVersionId must be sent too.

    public const string SchemaName       = $"{SystemPrefix}schema.name";         // EVENT TYPE - only required for old dbs
    public const string SchemaDataFormat = $"{SystemPrefix}schema.data-format";  // NEW always required
    public const string SchemaVersionId  = $"{SystemPrefix}schema.version-id";   // NEW only used when schema registry is supported with new client

    public const string SchemaUrn  = $"{SystemPrefix}schema.urn";   // NEW represents the schema name + format + version id

    // NEW required for old dbs and new client, can be removed later automatically when writting to a new d
    public const string HasProperties  = $"{SystemPrefix}has-properties";
}
