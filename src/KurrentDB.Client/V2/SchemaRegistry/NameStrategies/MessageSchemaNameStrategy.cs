namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// A schema naming strategy that generates schema names based on the type's full name,
/// optionally formatted according to the specified schema name format.
/// </summary>
/// <param name="format">The format specification for the schema name, determining how the schema name is structured.</param>
public class MessageSchemaNameStrategy(bool urnFormat = false) : SchemaNameStrategyBase(urnFormat ? SchemaNameOutputFormat.Urn : SchemaNameOutputFormat.None) {
	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) {
		return (messageType.Namespace!, messageType.Name);
	}
}
