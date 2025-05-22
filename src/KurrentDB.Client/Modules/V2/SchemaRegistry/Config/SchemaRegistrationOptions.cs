
namespace KurrentDB.Client.SchemaRegistry;

/// <summary>
/// Configuration for schema registration behavior
/// </summary>
public record SchemaRegistrationOptions {
	/// <summary>
	/// Controls schema registration behavior
	/// </summary>
	public SchemaRegistrationMode RegistrationMode { get; init; } = SchemaRegistrationMode.Manual;

	/// <summary>
	/// Controls schema validation behavior.
	/// </summary>
	public SchemaValidationMode ValidationMode { get; init; } = SchemaValidationMode.Disabled;

	/// <summary>
	/// Controls whether to automatically map messages (CLR types) to schema names.
	/// When enabled, types are mapped to schema names for serialization and deserialization.
	/// </summary>
	public bool AutoMapMessages { get; init; } = true;

	/// <summary>
	/// Specifies the strategy used for generating schema names during serialization and deserialization.
	/// The schema naming strategy determines how the name of a schema is derived based on the message type
	/// and other possible context, ensuring consistent and clear identification of schemas across different systems.
	/// </summary>
	public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();

	public static SchemaRegistrationOptions AutoMap => new() {
		AutoMapMessages  = true,
		RegistrationMode = SchemaRegistrationMode.Manual,
		ValidationMode   = SchemaValidationMode.Disabled
	};

	public static SchemaRegistrationOptions ManualMap => new() {
		AutoMapMessages  = false,
		RegistrationMode = SchemaRegistrationMode.Manual,
		ValidationMode   = SchemaValidationMode.Disabled
	};

	public static SchemaRegistrationOptions AutoRegister => new() {
		AutoMapMessages  = true,
		RegistrationMode = SchemaRegistrationMode.Auto,
		ValidationMode   = SchemaValidationMode.Enabled
	};

	public static SchemaRegistrationOptions ManualRegister => new() {
		AutoMapMessages  = false,
		RegistrationMode = SchemaRegistrationMode.Manual,
		ValidationMode   = SchemaValidationMode.Enabled
	};

	public static SchemaRegistrationOptions ServerPolicy => new() {
		AutoMapMessages  = true,
		RegistrationMode = SchemaRegistrationMode.ServerPolicy,
		ValidationMode   = SchemaValidationMode.Enabled
	};
}

/// <summary>
/// Defines how schema registration should be handled
/// </summary>
public enum SchemaRegistrationMode {
	/// <summary>
	/// Automatically register schemas when they are first encountered
	/// </summary>
	Auto,

	/// <summary>
	/// Do not register schemas automatically - schemas must be pre-registered
	/// </summary>
	Manual,

	/// <summary>
	/// Let the server policy determine the registration behavior
	/// </summary>
	ServerPolicy
}

/// <summary>
/// Defines how schema validation should be handled
/// </summary>
public enum SchemaValidationMode {
	/// <summary>
	/// Validate schemas against the registry
	/// </summary>
	Enabled,

	/// <summary>
	/// Skip validation against the registry
	/// </summary>
	Disabled,

	/// <summary>
	/// Let the server policy determine the validation behavior
	/// </summary>
	ServerPolicy
}
