using JetBrains.Annotations;

namespace KurrentDB.Client;

[PublicAPI]
public static class SystemMetadataKeys {
    public const string SystemPrefix = "$";

    public const string ClientName     = $"{SystemPrefix}client.name";
    public const string ClientVersion  = $"{SystemPrefix}client.version";
    public const string ConnectionName = $"{SystemPrefix}connection.name";

    public const string SchemaName       = $"{SystemPrefix}schema.name";         // EVENT TYPE - only required for old dbs
    public const string SchemaDataFormat = $"{SystemPrefix}schema.data-format";  // NEW always required
    public const string SchemaVersionId  = $"{SystemPrefix}schema.version-id";   // NEW only used when schema registry is supported with new client

    public const string SchemaUrn  = $"{SystemPrefix}schema.urn";   // NEW represents the schema name + format + version id

    public const string Stream = $"{SystemPrefix}stream.id";


    // NEW required for old dbs and new client, can be removed later automatically when writting to a new d
    public const string HasProperties  = $"{SystemPrefix}has-properties";
}
