using System.Collections.Concurrent;

namespace KurrentDB.Client.Schema;

public class MessageTypeRegistry {
	readonly ConcurrentDictionary<string, Type?> _typeMap     = new();
	readonly ConcurrentDictionary<Type, string>  _typeNameMap = new();

	public void Register(string messageTypeName, Type messageType) {
		_typeNameMap.AddOrUpdate(messageType, messageTypeName, (_, _) => messageTypeName);
		_typeMap.AddOrUpdate(messageTypeName, messageType, (_, type) => type);
	}

	public string? GetTypeName(Type messageType) {
#if NET48
		return _typeNameMap.TryGetValue(messageType, out var value) ? value : null;
#else
		return _typeNameMap.GetValueOrDefault(messageType);
#endif
	}

	public string GetOrAddTypeName(Type clrType, Func<Type, string> getTypeName) =>
		_typeNameMap.GetOrAdd(
			clrType,
			_ => {
				var typeName = getTypeName(clrType);
				_typeMap.TryAdd(typeName, clrType);
				return typeName;
			}
		);

	public Type? GetClrType(string messageTypeName) {
#if NET48
		return _typeMap.TryGetValue(messageTypeName, out var value) ? value : null;
#else
		return _typeMap.GetValueOrDefault(messageTypeName);
#endif
	}

	public Type? GetOrAddClrType(string messageTypeName, Func<string, Type?> getClrType) =>
		_typeMap.GetOrAdd(
			messageTypeName,
			_ => {
				var clrType = getClrType(messageTypeName);

				if (clrType is null)
					return null;

				_typeNameMap.TryAdd(clrType, messageTypeName);

				return clrType;
			}
		);
}

static class MessageTypeRegistryExtensions {
	public static void Register<T>(this MessageTypeRegistry messageTypeRegistry, string messageTypeName) =>
		messageTypeRegistry.Register(messageTypeName, typeof(T));

	public static void Register(this MessageTypeRegistry messageTypeRegistry, IDictionary<string, Type> typeMap) {
		foreach (var map in typeMap) messageTypeRegistry.Register(map.Key, map.Value);
	}

	public static string? GetTypeName<TMessageType>(this MessageTypeRegistry messageTypeRegistry) =>
		messageTypeRegistry.GetTypeName(typeof(TMessageType));
}
