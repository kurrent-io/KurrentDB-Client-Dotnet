namespace Kurrent.Client.Schema.NameStrategies;

/// <summary>
/// A schema naming strategy that uses the provided namespace identifier combined with the message type name
/// to generate a unique schema name.
/// </summary>
public class NamespaceSchemaNameStrategy : SchemaNameStrategyBase {
	readonly string _namespaceIdentifier;

	/// <summary>
	/// A schema naming strategy that uses the provided namespace identifier combined with the message type name
	/// to generate a unique schema name.
	/// </summary>
	/// <param name="namespaceIdentifier">The fixed namespace identifier to be used as part of the schema name.</param>
	/// <param name="format">The format specification for the schema name, determining how the schema name is structured.</param>
	/// <exception cref="ArgumentException">Thrown if the namespace identifier is empty or white space.</exception>
	public NamespaceSchemaNameStrategy(string namespaceIdentifier, SchemaNameOutputFormat format = SchemaNameOutputFormat.None) : base(format) {
		if (string.IsNullOrWhiteSpace(namespaceIdentifier))
			throw new ArgumentException("Namespace Identifier cannot be null or whitespace", nameof(namespaceIdentifier));

		_namespaceIdentifier = namespaceIdentifier;
	}

	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) =>
		(_namespaceIdentifier, messageType.Name);
}
