using Kurrent.Client.Registry;
using Kurrent.Client.Schema.NameStrategies;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Schema;

/// <summary>
/// Contains the resolved policy decisions for schema registry operations
/// after evaluating client options against server requirements.
/// </summary>
public record ResolvedSchemaRegistryPolicy {
	internal ResolvedSchemaRegistryPolicy(
		bool autoRegisterSchemas,
		bool validateSchemas,
		bool autoMapMessages,
		SchemaDataFormat dataFormat,
		ISchemaNameStrategy schemaNameStrategy,
		string? stream
	) {
		AutoRegisterSchemas = autoRegisterSchemas;
		ValidateSchemas     = validateSchemas;
		AutoMapMessages     = autoMapMessages;
		DataFormat          = dataFormat;
		SchemaNameStrategy  = schemaNameStrategy;
		Stream              = stream;
	}

	/// <summary>
	/// Whether schemas should be automatically registered
	/// </summary>
	public bool AutoRegisterSchemas { get; private init; }

	/// <summary>
	/// Whether schemas should be validated
	/// </summary>
	public bool ValidateSchemas { get; private init; }

	/// <summary>
	/// Whether to automatically map messages (CLR types) to schema names.
	/// This allows for automatic serialization and deserialization of messages,
	/// and is the default behavior.
	/// </summary>
	public bool AutoMapMessages { get; private init; }

	/// <summary>
	/// The schema data format to use for serialization and deserialization.
	/// This is determined by the server policy and may be enforced.
	/// If the server does not enforce a specific format, this will be unspecified.
	/// </summary>
	public SchemaDataFormat DataFormat { get; private init; }

	/// <summary>
	/// Determines the strategy used for generating schema names for messages in schema registry interactions.
	/// </summary>
	public ISchemaNameStrategy SchemaNameStrategy { get; private init; }

	/// <summary>
	/// The stream identifier associated with the schema. This property
	/// is used to define schema uniqueness or mapping in the context
	/// of specific streams when generating schema names or validating
	/// schema-related operations.
	/// </summary>
	public string? Stream { get; private init; }

	/// <summary>
	/// Whether any schema registry operations are needed
	/// </summary>
	public bool SchemaRegistryRequired => AutoRegisterSchemas || ValidateSchemas;

	/// <summary>
	/// Ensures the data format is allowed by the policy
	/// </summary>
	public ResolvedSchemaRegistryPolicy EnsureDataFormatCompliance(SchemaDataFormat dataFormat) {
		if (DataFormat != SchemaDataFormat.Unspecified && DataFormat != dataFormat)
			throw new NonCompliantSchemaDataFormatException(dataFormat, DataFormat);

		return this;
	}

	public SchemaName GetSchemaName(Type messageType) =>
		SchemaNameStrategy.GenerateSchemaName(messageType, Stream);
}

public delegate SchemaName GenerateSchemaName(Type messageType);

/// <summary>
/// Exception thrown when a schema data format does not comply with the format required by the schema registry governance policy.
/// </summary>
public class NonCompliantSchemaDataFormatException : KurrentException {
	/// <summary>
	/// Initializes a new instance of the <see cref="NonCompliantSchemaDataFormatException"/> class.
	/// </summary>
	/// <param name="providedFormat">The data format that was provided but does not comply with policy.</param>
	/// <param name="requiredFormat">The data format that is required according to the schema registry governance policy.</param>
	public NonCompliantSchemaDataFormatException(SchemaDataFormat providedFormat, SchemaDataFormat requiredFormat)
		: base(ErrorMessage(providedFormat, requiredFormat)) {
		ProvidedFormat = providedFormat;
		RequiredFormat = requiredFormat;
	}

	/// <summary>
	/// The data format that was provided but does not comply with policy.
	/// </summary>
	public SchemaDataFormat ProvidedFormat { get; }

	/// <summary>
	/// The data format required according to the schema registry governance policy.
	/// </summary>
	public SchemaDataFormat RequiredFormat { get; }

	static string ErrorMessage(SchemaDataFormat providedFormat, SchemaDataFormat requiredFormat) =>
		$"Schema registry is enabled and data format enforcement is active. " +
		$"The data format '{providedFormat}' is not allowed. It must be '{requiredFormat}'.";
}
