namespace KurrentDB.Client.Model;

public record RegisteredSchema {
    public static readonly RegisteredSchema None = new();

    public string           SchemaName      { get; init; } = null!;
    public SchemaDataFormat DataFormat      { get; init; }
    public string           SchemaVersionId { get; init; } = null!;
    public string           Definition      { get; init; } = null!;
    public int              VersionNumber   { get; init; }
    public DateTimeOffset   CreatedAt       { get; init; }

    public SchemaInfo ToSchemaInfo() => new(SchemaName, DataFormat);
}