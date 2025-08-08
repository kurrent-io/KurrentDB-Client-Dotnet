using Kurrent.Client.Registry;

#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace Kurrent.Client.Schema.NameStrategies;

/// <summary>
/// Defines the format options for schema name generation, which determine
/// how schema names are composed and represented.
/// </summary>
public enum SchemaNameOutputFormat {
	None      = 0,
	KebabCase = 1,
	SnakeCase = 2,
	Urn       = 3
}

public interface ISchemaNameStrategy {
	/// <summary>
	/// Generates a schema name based on the specified message type and optional stream name,
	/// formatted according to the schema naming strategy.
	/// </summary>
	/// <param name="messageType">The type of the message for which the schema name is being generated. Must not be null.</param>
	/// <param name="stream">An optional stream name that influences the schema name generation. Defaults to null.</param>
	/// <returns>The generated schema name as a string, formatted according to the specified schema name format.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the provided message type is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the schema name format is unsupported.</exception>
	SchemaName GenerateSchemaName(Type messageType, string? stream = null);
}

/// <summary>
/// Base class for schema naming strategies
/// </summary>
public abstract class SchemaNameStrategyBase(SchemaNameOutputFormat format) : ISchemaNameStrategy {
	/// <summary>
	/// Generates a schema name based on the specified message type and optional stream name,
	/// formatted according to the schema naming strategy.
	/// </summary>
	/// <param name="messageType">The type of the message for which the schema name is being generated. Must not be null.</param>
	/// <param name="stream">An optional stream name that influences the schema name generation. Defaults to null.</param>
	/// <returns>The generated schema name as a string, formatted according to the specified schema name format.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the provided message type is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the schema name format is unsupported.</exception>
	public SchemaName GenerateSchemaName(Type messageType, string? stream = null) {
		if (messageType is null || messageType == SystemTypes.MissingType)
			throw new ArgumentNullException(nameof(messageType));

		var (ns, msg) = Generate(messageType, stream ?? string.Empty);

		return format switch {
			SchemaNameOutputFormat.None      => $"{ns}.{msg}",
			SchemaNameOutputFormat.KebabCase => $"{ns.ToKebabCase()}.{msg.ToKebabCase()}",
			SchemaNameOutputFormat.SnakeCase => $"{ns.ToSnakeCase()}.{msg.ToSnakeCase()}",
			SchemaNameOutputFormat.Urn       => SchemaUrn.Create(ns.ToKebabCase(), msg.ToKebabCase())
		};
	}

	protected abstract (string Namespace, string MessageName) Generate(Type messageType, string streamName);
}
