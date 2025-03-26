using System.Diagnostics.CodeAnalysis;
using Kurrent.Diagnostics.Tracing;

namespace KurrentDB.Client.Core.Serialization;

public interface IMessageTypeNamingStrategy {
	string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext);

#if NET48
	bool TryResolveClrType(EventRecord record, out Type? type);
#else
	bool TryResolveClrType(EventRecord messageTypeName, [NotNullWhen(true)] out Type? type);
#endif

#if NET48
	bool TryResolveClrMetadataType(EventRecord record, out Type? type);
#else
	bool TryResolveClrMetadataType(EventRecord messageTypeName, [NotNullWhen(true)] out Type? type);
#endif
}

public record MessageTypeNamingResolutionContext(string CategoryName) {
	public static MessageTypeNamingResolutionContext FromStreamName(string streamName) =>
		new(streamName.Split('-').FirstOrDefault() ?? "no_stream_category");
}

class MessageTypeNamingStrategyWrapper(
	IMessageTypeRegistry messageTypeRegistry,
	IMessageTypeNamingStrategy messageTypeNamingStrategy
) : IMessageTypeNamingStrategy {
	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) {
		return messageTypeRegistry.GetOrAddTypeName(
			messageType,
			_ => messageTypeNamingStrategy.ResolveTypeName(messageType, resolutionContext)
		);
	}

#if NET48
	public bool TryResolveClrType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetOrAddClrType(
			record.EventType,
			_ => messageTypeNamingStrategy.TryResolveClrType(record, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}

#if NET48
	public bool TryResolveClrMetadataType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrMetadataType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		type = messageTypeRegistry.GetOrAddClrType(
			$"{record}-metadata",
			_ => messageTypeNamingStrategy.TryResolveClrMetadataType(record, out var resolvedType)
				? resolvedType
				: null
		);

		return type != null;
	}
}

public class DefaultMessageTypeNamingStrategy(Type? defaultMetadataType) : IMessageTypeNamingStrategy {
	readonly Type _defaultMetadataType = defaultMetadataType ?? typeof(TracingMetadata);

	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		$"{resolutionContext.CategoryName}-{messageType.FullName}";

#if NET48
	public bool TryResolveClrType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		var messageTypeName        = record.EventType;
		var categorySeparatorIndex = messageTypeName.IndexOf('-');

		if (categorySeparatorIndex == -1 || categorySeparatorIndex == messageTypeName.Length - 1) {
			type = null;
			return false;
		}

		var clrTypeName = messageTypeName[(categorySeparatorIndex + 1)..];

		type = TypeProvider.GetTypeByFullName(clrTypeName);

		return type != null;
	}

#if NET48
	public bool TryResolveClrMetadataType(EventRecord record, out Type? type) {
#else
	public bool TryResolveClrMetadataType(EventRecord record, [NotNullWhen(true)] out Type? type) {
#endif
		type = _defaultMetadataType;
		return true;
	}
}
