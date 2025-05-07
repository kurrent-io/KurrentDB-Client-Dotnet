
namespace KurrentDB.Client.Canvas.triage;

// public interface IMessageTypeNamingStrategy {
// 	string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext);
//
// 	bool TryResolveClrTypeName(EventRecord record, out string? clrTypeName);
//
// 	bool TryResolveClrMetadataTypeName(EventRecord record, out string? clrTypeName);
// }
//
record MessageTypeNamingResolutionContext(string CategoryName) {
	public static MessageTypeNamingResolutionContext FromStreamName(string streamName) =>
		new(streamName.Split('-').FirstOrDefault() ?? "no_stream_category");
}

class DefaultMessageTypeNamingStrategy(Type? defaultMetadataType) {
	readonly Type _defaultMetadataType = defaultMetadataType!;  // typeof(TracingMetadata);

	public string ResolveTypeName(Type messageType, MessageTypeNamingResolutionContext resolutionContext) =>
		$"{resolutionContext.CategoryName}-{messageType.FullName}";

	public bool TryResolveClrTypeName(EventRecord record, out string? clrTypeName) {
		var messageTypeName        = record.EventType;
		var categorySeparatorIndex = messageTypeName.IndexOf('-');

		if (categorySeparatorIndex == -1 || categorySeparatorIndex == messageTypeName.Length - 1) {
			clrTypeName = null;
			return false;
		}

		clrTypeName = messageTypeName[(categorySeparatorIndex + 1)..];

		return true;
	}

	public bool TryResolveClrMetadataTypeName(EventRecord record, out string? clrTypeName) {
		clrTypeName = _defaultMetadataType.FullName;
		return clrTypeName != null;
	}
}
