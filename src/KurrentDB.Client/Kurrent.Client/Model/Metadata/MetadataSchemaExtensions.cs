using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Model;

[PublicAPI]
public static class MetadataSchemaExtensions {
	public static RecordSchemaInfo GetSchemaInfo(this Metadata metadata) =>
		new(GetSchemaName(metadata), GetSchemaDataFormat(metadata), GetSchemaVersionId(metadata));

	public static SchemaName GetSchemaName(this Metadata metadata) =>
		metadata.GetOrDefault(SystemMetadataKeys.SchemaName, SchemaName.None);

	public static SchemaDataFormat GetSchemaDataFormat(this Metadata metadata) =>
		metadata.GetOrDefault(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified);

	public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) =>
		metadata.GetOrDefault(SystemMetadataKeys.SchemaVersionId, SchemaVersionId.None);

	public static Metadata AddSchemaNameIfMissing(this Metadata metadata, SchemaName schemaName) =>
		metadata.WithIf(x => x.GetSchemaName() == SchemaName.None, SystemMetadataKeys.SchemaName, schemaName);

	public static Metadata AddSchemaDataFormatIfMissing(this Metadata metadata, SchemaDataFormat dataFormat) {
		return dataFormat != SchemaDataFormat.Unspecified
			? metadata.WithIf(x => x.GetSchemaDataFormat() == SchemaDataFormat.Unspecified, SystemMetadataKeys.SchemaDataFormat, dataFormat)
			: throw new ArgumentOutOfRangeException(nameof(dataFormat), "SchemaDataFormat cannot be Unspecified when adding to metadata.");
	}
}
