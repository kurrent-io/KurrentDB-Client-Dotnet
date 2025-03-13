// ReSharper disable InconsistentNaming

using KurrentDB.Client;

namespace KurrentDB.Client.Tests.Streams;

public record SubscriptionFilter(string Name, Func<string, IEventFilter> Create, Func<string, MessageData, MessageData> PrepareEvent) {
	public override string ToString() => Name;

	static readonly SubscriptionFilter StreamNamePrefix = new(nameof(StreamNamePrefix), StreamFilter.Prefix, (_, evt) => evt);
	static readonly SubscriptionFilter StreamNameRegex  = new(nameof(StreamNameRegex), f => StreamFilter.RegularExpression(f), (_, evt) => evt);
	static readonly SubscriptionFilter EventTypePrefix  = new(nameof(EventTypePrefix), EventTypeFilter.Prefix, (term, evt) => new(term, evt.Data, evt.Metadata, evt.MessageId, evt.ContentType));
	static readonly SubscriptionFilter EventTypeRegex   = new(nameof(EventTypeRegex), f => EventTypeFilter.RegularExpression(f), (term, evt) => new(term, evt.Data, evt.Metadata, evt.MessageId, evt.ContentType));

	static SubscriptionFilter() {
		All = new[] {
			StreamNamePrefix,
			StreamNameRegex,
			EventTypePrefix,
			EventTypeRegex
		};

		TestCases = All.Select(x => new object[] { x });
	}

	public static SubscriptionFilter[]   All       { get; }
	public static IEnumerable<object?[]> TestCases { get; }
}
