namespace Kurrent.Client.Streams;

public static class SystemConstants {
	[PublicAPI]
	public static class MetadataKeys {
		const string SystemPrefix = "$";

		public static bool IsSystemMetadataKey(string key) =>
			key.StartsWith(SystemPrefix, StringComparison.Ordinal);

		public const string SchemaName       = $"{SystemPrefix}schema.name";
		public const string SchemaDataFormat = $"{SystemPrefix}schema.data-format";
		public const string SchemaVersionId  = $"{SystemPrefix}schema.version-id";
		public const string SchemaUrn        = $"{SystemPrefix}schema.urn"; // NEW represents the schema name + format and version id

		#region . internal .

		/// <summary>
		/// possibly used internally and then removed right after.
		/// </summary>
		internal const string Stream = $"{SystemPrefix}stream.name";

		/// <summary>
		/// not sure yet, but will be required when using the new contracts because
		/// old metadata will be sent inside the new Record properties
		/// </summary>
		internal const string HasProperties = $"{SystemPrefix}has-properties";

		#endregion
	}

	public static class EventSchemaNames {
		///<summary>
		/// event type for stream deleted
		///</summary>
		public const string StreamDeleted = "$streamDeleted";

		///<summary>
		/// event type for statistics
		///</summary>
		public const string StatsCollection = "$statsCollected";

		///<summary>
		/// event type for stream metadata
		///</summary>
		public const string StreamMetadata = "$metadata";

		///<summary>
		/// event type for the system settings
		///</summary>
		public const string Settings = "$settings";
	}
}
