using FluentValidation;
using FluentValidation.Results;
using Kurrent.Client.SchemaRegistry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kurrent.Client;

/// <summary>
/// Defines schema settings for a KurrentDB client, including naming strategies and auto-registration.
/// </summary>
/// <remarks>
/// <para>
/// This record provides configuration options for schema handling, validation, and registration
/// strategies when working with message schemas in KurrentDB.
/// </para>
/// <para>
/// Schema options control how the client interacts with the schema registry, including whether
/// schemas are automatically registered, validated, and how schema names are generated.
/// </para>
/// <para>
/// Several predefined configurations are available as static properties: <see cref="FullValidation"/>,
/// <see cref="NoAutomaticRegistration"/>, <see cref="NoValidation"/>, and <see cref="Disabled"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create custom schema options
/// var schemaOptions = new KurrentClientSchemaOptions {
///     AutoRegister = true,
///     Validate = true,
///     SchemaNameStrategy = new CustomSchemaNameStrategy()
/// };
///
/// // Or use predefined options
/// var client = new KurrentClient(connectionSettings, KurrentClientSchemaOptions.Default);
/// </code>
/// </example>
[PublicAPI]
public record KurrentClientSchemaOptions : KurrentClientOptionsBase {
	/// <summary>
	/// Whether schemas should be automatically registered.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When set to <see langword="true"/>, the client will automatically register schemas for message types
	/// the first time they are encountered. When <see langword="false"/>, schemas must be registered manually.
	/// </para>
	/// <para>
	/// Automatic registration simplifies development but may not be suitable for production environments
	/// where schema changes should be more strictly controlled.
	/// </para>
	/// <para>
	/// Defaults to <see langword="true"/>.
	/// </para>
	/// </remarks>
	public bool AutoRegister { get; init; } = true;

	/// <summary>
	/// Whether messages should be validated against their schemas.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When set to <see langword="true"/>, messages will be validated against their registered schemas before being sent.
	/// And when reading messages, the client will ensure they conform to the expected schema.
	/// This helps ensure data integrity but may impact performance.
	/// </para>
	/// <para>
	/// Validation provides an additional safety layer to ensure that messages conform to their expected schema
	/// before they are sent to the server.
	/// </para>
	/// <para>
	/// Defaults to <see langword="true"/>.
	/// </para>
	/// </remarks>
	public bool Validate { get; init; } = true;

	/// <summary>
	/// The strategy used for generating schema names.
	/// This determines how schemas are named when registering message types.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The schema name strategy controls how the client generates schema names for message types.
	/// The default implementation, <see cref="MessageSchemaNameStrategy"/>, uses the message type's
	/// namespace and name to create schema identifiers.
	/// </para>
	/// <para>
	/// Custom strategies can be implemented by creating classes that implement the <see cref="ISchemaNameStrategy"/> interface.
	/// </para>
	/// <para>
	/// Defaults to <see cref="MessageSchemaNameStrategy"/>, which enables zero-code automatic serialization and deserialization.
	/// </para>
	/// </remarks>
	public ISchemaNameStrategy NameStrategy { get; init; } = new MessageSchemaNameStrategy();

	/// <summary>
	/// The default schema registry settings with all standard options enabled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Provides a preconfigured set of schema options with all standard features enabled:
	/// </para>
	/// <list type="table">
	///   <item><description><see cref="MessageSchemaNameStrategy"/> for schema naming</description></item>
	///   <item><description>Automatic schema registration enabled</description></item>
	///   <item><description>Schema validation enabled</description></item>
	/// </list>
	/// <para>
	/// This is the most convenient option for development and testing scenarios.
	/// </para>
	/// </remarks>
	/// <returns>
	/// A new <see cref="KurrentClientSchemaOptions"/> instance with default settings.
	/// </returns>
	public static KurrentClientSchemaOptions FullValidation => new();

	/// <summary>
	/// Schema options with automatic registration disabled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Provides a preconfigured set of schema options with automatic registration disabled.
	/// Schema validation remains enabled.
	/// </para>
	/// <para>
	/// Use this configuration when you want to manually control schema registration
	/// but still benefit from schema validation.
	/// </para>
	/// </remarks>
	/// <returns>
	/// A new <see cref="KurrentClientSchemaOptions"/> instance with auto-registration disabled.
	/// </returns>
	public static KurrentClientSchemaOptions NoAutomaticRegistration => new() {
		AutoRegister = false
	};

	/// <summary>
	/// Schema options with validation disabled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Provides a preconfigured set of schema options with validation disabled.
	/// Automatic schema registration remains enabled.
	/// </para>
	/// <para>
	/// Use this configuration in performance-sensitive scenarios where you trust
	/// the message data and want to avoid the overhead of schema validation.
	/// </para>
	/// </remarks>
	/// <returns>
	/// A new <see cref="KurrentClientSchemaOptions"/> instance with validation disabled.
	/// </returns>
	/// <example>
	/// <code>
	/// // Create a client that automatically registers schemas but skips validation
	/// var client = new KurrentClient(connectionSettings, KurrentClientSchemaOptions.NoValidation);
	/// </code>
	/// </example>
	public static KurrentClientSchemaOptions NoValidation => new() {
		Validate = false
	};

	/// <summary>
	/// Schema options with both automatic registration and validation disabled.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Provides a preconfigured set of schema options with both automatic registration
	/// and validation disabled.
	/// </para>
	/// <para>
	/// This is the most lightweight configuration and is suitable for advanced scenarios
	/// where you want complete control over schema handling or maximum performance.
	/// </para>
	/// <para>
	/// <b>Note:</b> With this configuration, you are responsible for ensuring message
	/// compatibility with the schema registry.
	/// </para>
	/// </remarks>
	/// <returns>
	/// A new <see cref="KurrentClientSchemaOptions"/> instance with both auto-registration
	/// and validation disabled.
	/// </returns>
	public static KurrentClientSchemaOptions Disabled => new() {
		AutoRegister = false,
		Validate     = false
	};
}
