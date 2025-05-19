namespace KurrentDB.Client.SchemaRegistry;

public interface ISchemaExporter {
	string ExportSchemaDefinition(Type messageType);

	// this is required, engine uses json schema for validation, but schema might be protobuf or something else
	//string ExportSchemaForValidation(Type messageType);
}
