using System.Reflection;
using KurrentDB.Client;

namespace KurrentDB.Client.Tests.PersistentSubscriptions;

public static class Filters {
	const string StreamNamePrefix = nameof(StreamNamePrefix);
	const string StreamNameRegex  = nameof(StreamNameRegex);
	const string EventTypePrefix  = nameof(EventTypePrefix);
	const string EventTypeRegex   = nameof(EventTypeRegex);

	static readonly IDictionary<string, (Func<string, IEventFilter>, Func<string, MessageData, MessageData>)>
		s_filters =
			new Dictionary<string, (Func<string, IEventFilter>, Func<string, MessageData, MessageData>)> {
				[StreamNamePrefix] = (StreamFilter.Prefix, (_, e) => e),
				[StreamNameRegex]  = (f => StreamFilter.RegularExpression(f), (_, e) => e),
				[EventTypePrefix] = (EventTypeFilter.Prefix, (term, e) => new(
					term,
					e.Data,
					e.Metadata,
					e.MessageId,
					e.ContentType
				)),
				[EventTypeRegex] = (f => EventTypeFilter.RegularExpression(f), (term, e) => new(
					term,
					e.Data,
					e.Metadata,
					e.MessageId,
					e.ContentType
				))
			};

	public static readonly IEnumerable<string> All = typeof(Filters)
		.GetFields(BindingFlags.NonPublic | BindingFlags.Static)
		.Where(fi => fi.IsLiteral && !fi.IsInitOnly)
		.Select(fi => (string)fi.GetRawConstantValue()!);

	public static (Func<string, IEventFilter> getFilter, Func<string, MessageData, MessageData> prepareEvent)
		GetFilter(string name) =>
		s_filters[name];
}
