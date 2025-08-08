namespace Kurrent.Client.Registry;

public readonly record struct SchemaVersionDescriptor(SchemaVersionId VersionId, int VersionNumber) {
    public static readonly SchemaVersionDescriptor None = new(SchemaVersionId.None, 0);
}
