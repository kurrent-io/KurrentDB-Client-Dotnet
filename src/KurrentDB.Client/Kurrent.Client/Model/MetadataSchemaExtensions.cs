using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Model;

public static class MetadataSchemaExtensions {
	public static RecordSchemaInfo GetSchemaInfo(this Metadata metadata) =>
		new(GetSchemaName(metadata), GetSchemaDataFormat(metadata), GetSchemaVersionId(metadata));

	public static SchemaName GetSchemaName(this Metadata metadata) =>
		metadata.TryGetSchemaName(out var schemaName) ? schemaName : SchemaName.None;

	public static SchemaDataFormat GetSchemaDataFormat(this Metadata metadata) =>
		metadata.GetOrDefault(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified);

	public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) =>
		metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionId)
			? SchemaVersionId.From(schemaVersionId!)
			: SchemaVersionId.None;

	public static bool TryGetSchemaName(this Metadata metadata, out SchemaName schemaName) {
		if (metadata.TryGet<string>(SystemMetadataKeys.SchemaName, out var value)) {
			schemaName = SchemaName.From(value!);
			return true;
		}

		schemaName = SchemaName.None;
		return false;
	}
}
