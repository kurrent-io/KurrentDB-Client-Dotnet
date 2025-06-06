namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// A schema naming strategy that generates schema names based on the type's full name,
/// optionally formatted according to the specified schema name format.
/// </summary>
/// <param name="urnFormat">The format specification for the schema name, determining how the schema name is structured.</param>
public class MessageSchemaNameStrategy(bool urnFormat = false) : SchemaNameStrategyBase(urnFormat ? SchemaNameOutputFormat.Urn : SchemaNameOutputFormat.None) {
	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) {
		return (messageType.Namespace!, messageType.Name);
	}
}

public class MessageSchemaNameStrategyWithFormat(SchemaNameOutputFormat format) : SchemaNameStrategyBase(format) {
	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) =>
		(messageType.Namespace!, messageType.Name);
}
