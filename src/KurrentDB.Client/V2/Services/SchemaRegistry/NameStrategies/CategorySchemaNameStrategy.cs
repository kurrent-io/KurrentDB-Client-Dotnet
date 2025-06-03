namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Schema naming strategy that derives the schema name based on a specified
/// category from the stream name and the name of the message type.
/// </summary>
/// <remarks>
/// This strategy uses the first segment of the stream name, separated by a hyphen ('-'),
/// as the namespace identifier and combines it with the name of the message type.
/// A non-empty stream name is required for this strategy to function correctly.
/// </remarks>
/// <param name="format">The format specification for the schema name, determining how the schema name is structured.</param>
/// <exception cref="ArgumentException">Thrown if the stream name is empty or white space.</exception>
public class CategorySchemaNameStrategy(SchemaNameOutputFormat format = SchemaNameOutputFormat.None) : SchemaNameStrategyBase(format) {
	protected override (string Namespace, string MessageName) Generate(Type messageType, string streamName) {
		if (string.IsNullOrWhiteSpace(streamName))
			throw new ArgumentException("Stream name cannot be empty or whitespace", nameof(streamName));

		return (streamName.Split('-').First(), messageType.Name);
	}
}
