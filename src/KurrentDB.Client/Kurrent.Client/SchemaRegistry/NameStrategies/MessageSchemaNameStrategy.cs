namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// A schema naming strategy that generates schema names based on the type's full name,
/// optionally formatted according to the specified schema name format.
/// </summary>
/// <param name="format">The format to apply to the schema name.</param>
public class MessageSchemaNameStrategy(SchemaNameOutputFormat format = SchemaNameOutputFormat.None) : SchemaNameStrategyBase(format) {
	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) => (messageType.Namespace!, messageType.Name);
}
