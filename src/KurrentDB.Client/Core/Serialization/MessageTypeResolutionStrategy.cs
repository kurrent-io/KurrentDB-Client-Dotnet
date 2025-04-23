using System.Diagnostics.CodeAnalysis;
using Kurrent.Diagnostics.Tracing;

namespace KurrentDB.Client.Core.Serialization;

public interface IMessageTypeNamingStrategy {
	string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext);

#if NET48
	bool TryResolveClrTypeName(EventRecord record, out string? clrTypeName);
#else
	bool TryResolveClrTypeName(EventRecord messageTypeName, [NotNullWhen(true)] out string? clrTypeName);
#endif

#if NET48
	bool TryResolveClrMetadataTypeName(EventRecord record, out string? clrTypeName);
#else
	bool TryResolveClrMetadataTypeName(EventRecord messageTypeName, [NotNullWhen(true)] out string? clrTypeName);
#endif
}

public record MessageTypeNamingResolutionContext(string CategoryName) {
	public static MessageTypeNamingResolutionContext FromStreamName(string streamName) =>
		new(streamName.Split('-').FirstOrDefault() ?? "no_stream_category");
}

public class DefaultMessageTypeNamingStrategy(Type? defaultMetadataType) : IMessageTypeNamingStrategy {
	readonly Type _defaultMetadataType = defaultMetadataType ?? typeof(TracingMetadata);

	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		$"{resolutionContext.CategoryName}-{messageType.FullName}";

#if NET48
	public bool TryResolveClrTypeName(EventRecord record, out string? clrTypeName) {
#else
	public bool TryResolveClrTypeName(EventRecord record, [NotNullWhen(true)] out string? clrTypeName) {
#endif
		var messageTypeName        = record.EventType;
		var categorySeparatorIndex = messageTypeName.IndexOf('-');

		if (categorySeparatorIndex == -1 || categorySeparatorIndex == messageTypeName.Length - 1) {
			clrTypeName = null;
			return false;
		}

		clrTypeName = messageTypeName[(categorySeparatorIndex + 1)..];

		return true;
	}

#if NET48
	public bool TryResolveClrMetadataTypeName(EventRecord record, out string? clrTypeName) {
#else
	public bool TryResolveClrMetadataTypeName(EventRecord record, [NotNullWhen(true)] out string? clrTypeName) {
#endif
		clrTypeName = _defaultMetadataType.FullName;
		return clrTypeName != null;
	}
}
