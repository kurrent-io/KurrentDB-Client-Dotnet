namespace KurrentDB.Client.Tests;

public readonly record struct MessageBinaryData(Uuid Id, byte[] Data, byte[] Metadata) {
	public bool Equals(MessageBinaryData other) =>
		Id.Equals(other.Id)
	 && Data.SequenceEqual(other.Data)
	 && Metadata.SequenceEqual(other.Metadata);

	public override int GetHashCode() => System.HashCode.Combine(Id, Data, Metadata);
}

public static class EventBinaryDataConverters {
	public static MessageBinaryData ToBinaryData(this MessageData source) =>
		new(source.MessageId, source.Data.ToArray(), source.Metadata.ToArray());

	public static MessageBinaryData ToBinaryData(this EventRecord source) =>
		new(source.EventId, source.Data.ToArray(), source.Metadata.ToArray());

	public static MessageBinaryData ToBinaryData(this ResolvedEvent source) =>
		source.Event.ToBinaryData();

	public static MessageBinaryData[] ToBinaryData(this IEnumerable<MessageData> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static MessageBinaryData[] ToBinaryData(this IEnumerable<EventRecord> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static MessageBinaryData[] ToBinaryData(this IEnumerable<ResolvedEvent> source) =>
		source.Select(x => x.ToBinaryData()).ToArray();

	public static ValueTask<MessageBinaryData[]> ToBinaryData(this IAsyncEnumerable<ResolvedEvent> source) =>
		source.DefaultIfEmpty().Select(x => x.ToBinaryData()).ToArrayAsync();
}
