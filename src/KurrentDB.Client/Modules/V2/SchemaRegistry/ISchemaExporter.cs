namespace KurrentDB.Client.SchemaRegistry;

public interface ISchemaExporter {
	string ExportSchemaDefinition(Type messageType);
	string ExportSchemaForValidation(Type messageType);
}
