using KurrentDB.Client.SchemaRegistry;

namespace KurrentDB.Client;

/// <summary>
/// Defines schema settings for a KurrentDB client, including naming strategies and auto-registration.
/// </summary>
public record KurrentDBClientSchemaRegistrySettings {
	public ISchemaNameStrategy NameStrategy { get; init; } = new MessageSchemaNameStrategy();
	public bool                AutoRegister { get; init; } = true;
	public bool                Validate     { get; init; } = true;

	public static KurrentDBClientSchemaRegistrySettings Default => new();
}
