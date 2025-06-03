// namespace Kurrent.Client.SchemaRegistry;
//
// /// <summary>
// /// Provides factory methods for creating instances of different schema name strategies.
// /// These strategies define how schema names are generated and represented based on the schema format and type-specific rules.
// /// </summary>
// static class SchemaNameStrategies {
// 	public static SchemaNameStrategyBase MessageSchemaNameStrategy(bool urnFormat = false) =>
// 		new MessageSchemaNameStrategy(urnFormat);
//
// 	public static SchemaNameStrategyBase CategorySchemaNameStrategy(SchemaNameOutputFormat format = SchemaNameOutputFormat.None) =>
// 		new CategorySchemaNameStrategy(format);
//
// 	public static SchemaNameStrategyBase NamespaceSchemaNameStrategy(string namespaceIdentifier, SchemaNameOutputFormat format = SchemaNameOutputFormat.None) =>
// 		new NamespaceSchemaNameStrategy(namespaceIdentifier, format);
//
// 	public static SchemaNameStrategyBase NamespaceCategorySchemaNameStrategy(string namespaceIdentifier, SchemaNameOutputFormat format = SchemaNameOutputFormat.None) =>
// 		new NamespaceCategorySchemaNameStrategy(namespaceIdentifier, format);
// }
