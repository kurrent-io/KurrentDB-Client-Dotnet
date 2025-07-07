using Kurrent.Client.SchemaRegistry;

namespace Kurrent.Client.Model;

[PublicAPI]
public static class MetadataSchemaExtensions {
	public static Metadata AddSchemaNameIfMissing(this Metadata metadata, SchemaName schemaName) =>
		metadata.WithIf(x => x.GetSchemaName() == SchemaName.None, SystemMetadataKeys.SchemaName, schemaName);

	public static Metadata AddSchemaDataFormatIfMissing(this Metadata metadata, SchemaDataFormat dataFormat) =>
		dataFormat != SchemaDataFormat.Unspecified
			? metadata.WithIf(x => x.GetSchemaDataFormat() == SchemaDataFormat.Unspecified, SystemMetadataKeys.SchemaDataFormat, dataFormat)
			: throw new ArgumentOutOfRangeException(nameof(dataFormat), "SchemaDataFormat cannot be Unspecified when adding to metadata.");

	public static RecordSchemaInfo GetSchemaInfo(this Metadata metadata) =>
		new(GetSchemaName(metadata), GetSchemaDataFormat(metadata), GetSchemaVersionId(metadata));

	public static SchemaName GetSchemaName(this Metadata metadata) =>
		metadata.TryGetSchemaName(out var schemaName) ? schemaName : SchemaName.None;

	public static SchemaDataFormat GetSchemaDataFormat(this Metadata metadata) =>
		metadata.GetOrDefault(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified);

	public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) =>
		metadata.TryGetSchemaVersionId(out var schemaVersionId) ? schemaVersionId : SchemaVersionId.None;

	// public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) {
	// 	return metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var value)
	// 		? SchemaVersionId.From(value?.Trim() ?? "")
	// 		: metadata.TryGet<SchemaVersionId>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionId)
	// 			? schemaVersionId
	// 			: SchemaVersionId.None;
	// }

	// public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) {
	// 	return metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var value)
	// 		? SchemaVersionId.From(value?.Trim() ?? "")
	// 		: metadata.TryGet<SchemaVersionId>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionId)
	// 			? schemaVersionId
	// 			: SchemaVersionId.None;
	// }

	static bool TryGetSchemaVersionId(this Metadata metadata, out SchemaVersionId schemaVersionId) {
		if (metadata.TryGet(SystemMetadataKeys.SchemaVersionId, out schemaVersionId))
			return true;

		// if (metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var value) && value is not null && Guid.TryParse(value, out var guid) && guid != Guid.Empty) {
		// 	schemaVersionId = SchemaVersionId.From(guid);
		// 	return true;
		// }

		if (metadata.TryGet<Guid>(SystemMetadataKeys.SchemaVersionId, out var guid) && guid != Guid.Empty) {
			schemaVersionId = SchemaVersionId.From(guid);
			return true;
		}

		schemaVersionId = SchemaVersionId.None;
		return false;
	}

	static bool TryGetSchemaName(this Metadata metadata, out SchemaName schemaName) {
		if (metadata.TryGet(SystemMetadataKeys.SchemaName, out schemaName))
			return true;

		if (metadata.TryGet<string>(SystemMetadataKeys.SchemaName, out var value) && !string.IsNullOrWhiteSpace(value)) {
			schemaName = SchemaName.From(value);
			return true;
		}

		schemaName = SchemaName.None;
		return false;

		// if (metadata.TryGet<string>(SystemMetadataKeys.SchemaName, out var value) && !string.IsNullOrWhiteSpace(value)) {
		// 	value = value?.Trim() ?? "";
		//
		// 	if (value == "") return false;
		//
		// 	schemaName = SchemaName.From(value);
		// 	return true;
		// }
		//
		// schemaName = SchemaName.None;
		// return false;
	}
}
