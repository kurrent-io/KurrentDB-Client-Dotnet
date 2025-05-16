namespace KurrentDB.Client.SchemaRegistry;

public interface IKurrentTypeResolver {
	Type ResolveMessageType(string schemaName);
}
