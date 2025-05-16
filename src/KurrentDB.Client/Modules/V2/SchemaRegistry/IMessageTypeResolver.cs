using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry;

/// <summary>
/// Provides an abstraction for resolving CLR types based on schema information and contextual details.
/// </summary>
public interface IMessageTypeResolver {
	/// <summary>
	/// Resolves the CLR type based on the provided schema name, stream name, and associated metadata.
	/// </summary>
	/// <param name="schemaName">The schema name to resolve into a CLR type.</param>
	/// <param name="stream">The name of the stream associated with the schema name.</param>
	/// <param name="metadata">A dictionary of additional metadata that influence the type resolution.</param>
	/// <returns>The resolved CLR type corresponding to the given schema name and stream.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the schema name or stream is null.</exception>
	/// <exception cref="KeyNotFoundException">Thrown when the schema name cannot be resolved to a type with the provided information.</exception>
	Type ResolveType(string schemaName, string stream, Metadata metadata);
}
