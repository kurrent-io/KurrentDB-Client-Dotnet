namespace KurrentDB.Client.SchemaRegistry;

public interface ITypeResolver {
	Type ResolveType(string schemaName);

	bool TryResolveType(string schemaName, out Type type);
}

public class DefaultTypeResolver : ITypeResolver {
	public Type ResolveType(string schemaName) =>
		Type.GetType(schemaName) ?? GetFirstMatchingTypeFromCurrentDomainAssembly(schemaName) ?? Type.Missing.GetType();

	public bool TryResolveType(string schemaName, out Type type) => throw new NotImplementedException();

	static Type? GetFirstMatchingTypeFromCurrentDomainAssembly(string fullName) {
		var firstNamespacePart = fullName.Split('.')[0];

		return AppDomain.CurrentDomain.GetAssemblies()
			.OrderByDescending(assembly => assembly.FullName?.StartsWith(firstNamespacePart) == true)
			.Select(assembly => assembly.GetType(fullName))
			.FirstOrDefault(type => type != null);
	}
}
