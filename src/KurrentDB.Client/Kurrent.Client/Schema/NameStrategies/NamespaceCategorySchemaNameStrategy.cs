namespace Kurrent.Client.Schema.NameStrategies;

/// <summary>
/// Schema naming strategy that generates schema names by combining a fixed namespace identifier with a category derived
/// from the stream name and the message type name.
/// </summary>
public class NamespaceCategorySchemaNameStrategy : SchemaNameStrategyBase {
	readonly string _namespaceIdentifier;

	/// <summary>
	/// Schema naming strategy that generates schema names by combining a fixed namespace identifier with a category derived
	/// from the stream name and the message type name.
	/// </summary>
	public NamespaceCategorySchemaNameStrategy(string namespaceIdentifier, SchemaNameOutputFormat format = SchemaNameOutputFormat.None) : base(format) {
		if (string.IsNullOrWhiteSpace(namespaceIdentifier))
			throw new ArgumentException("Namespace Identifier cannot be null or whitespace", nameof(namespaceIdentifier));

		_namespaceIdentifier = namespaceIdentifier;
	}

	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) {
		if (string.IsNullOrWhiteSpace(streamName))
			throw new ArgumentException("Stream name cannot be empty or whitespace", nameof(streamName));

		return ($"{_namespaceIdentifier}.{streamName.Split('-').First()}", messageType.Name);
	}
}
