using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry;

namespace KurrentDB.Client;

/// <summary>
/// Defines schema settings for a KurrentDB client, including naming strategies and auto-registration.
/// </summary>
public record KurrentDBClientSchemaRegistrySettings {
	/// <summary>
	/// Gets or initializes the strategy used for generating schema names.
	/// This determines how schemas are named when registering message types.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="MessageSchemaNameStrategy"/>, which uses the message type's namespace and name and allows zero code auto SerDe.
	/// </remarks>
	public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();

	/// <summary>
	/// Gets or initializes a value indicating whether schemas should be automatically registered.
	/// </summary>
	/// <remarks>
	/// When set to true, the client will automatically register schemas for message types
	/// the first time they are encountered. When false, schemas must be registered manually.
	/// Defaults to true.
	/// </remarks>
	public bool AutoRegister { get; init; } = true;

	/// <summary>
	/// Gets or initializes a value indicating whether messages should be validated against their schemas.
	/// </summary>
	/// <remarks>
	/// When set to true, messages will be validated against their registered schemas before being sent.
	/// This helps ensure data integrity but may impact performance.
	/// Defaults to true.
	/// </remarks>
	public bool Validate { get; init; } = true;

	/// <summary>
	/// Gets the default schema registry settings.
	/// </summary>
	/// <remarks>
	/// Uses default values for all properties:
	/// <see cref="MessageSchemaNameStrategy"/> for schema naming,
	/// automatic schema registration enabled, and
	/// schema validation enabled.
	/// </remarks>
	public static KurrentDBClientSchemaRegistrySettings Default => new();
}
