using System.Text.RegularExpressions;
using JetBrains.Annotations;
using static System.String;

namespace KurrentDB.Client.Model;

public enum ConsumeFilterScope {
    Unspecified = 0,
    Stream      = 1,
    Record      = 2
}

public enum ConsumeFilterType {
    Unspecified = 0,
    Regex       = 1,
    Prefix      = 2,
    StreamId    = 4
}

[PublicAPI]
public record ConsumeFilter {
    public static readonly ConsumeFilter None = new();

    public ConsumeFilterScope Scope    { get; private init; } = ConsumeFilterScope.Unspecified;
    public ConsumeFilterType  Type     { get; private init; } = ConsumeFilterType.Unspecified;
    public Regex              RegEx    { get; private init; } = new(Empty);

    public bool IsStreamFilter => Scope is ConsumeFilterScope.Stream;
    public bool IsRecordFilter => Scope is ConsumeFilterScope.Record;

    public bool IsStreamIdFilter => Type is ConsumeFilterType.StreamId;
    public bool IsRegexFilter    => Type is ConsumeFilterType.Regex;
    public bool IsPrefixFilter   => Type is ConsumeFilterType.Prefix;

    public bool IsEmptyFilter => Scope is ConsumeFilterScope.Unspecified
                              && Type  is ConsumeFilterType.Unspecified;

    public string Expression =>
        IsEmptyFilter ? Empty : RegEx.ToString();

    public override string ToString() => $"[{Scope}|{Type}] {Expression}";

    public bool IsMatch(ReadOnlySpan<char> input) {
#if NET48
	    return RegEx.IsMatch(new string(input.ToArray()));
#else
	    return RegEx.IsMatch(input);
#endif
    }

    public static ConsumeFilter FromStreamId(string streamId) =>
        new() {
            Scope = ConsumeFilterScope.Stream,
            Type  = ConsumeFilterType.StreamId,
            RegEx = new Regex(streamId)
        };

    public static ConsumeFilter FromPrefixes(ConsumeFilterScope scope, params string[] prefixes) {
        return new ConsumeFilter {
            Scope = scope,
            Type  = ConsumeFilterType.Prefix,
            RegEx = new Regex(CreatePattern(prefixes))
        };

        // Escape special characters in the prefixes and join them with '|'
        // Add '^' at the start to match the start of the string
        static string CreatePattern(string[] prefixes) =>
            $"^({Join("|", prefixes.Select(Regex.Escape))})";
    }

    public static ConsumeFilter FromPrefixes(ConsumeFilterScope scope, string expression) =>
        FromPrefixes(scope, expression.Split(','));

    public static ConsumeFilter FromRegex(ConsumeFilterScope scope, Regex regularExpression) =>
        new() {
            Scope = scope,
            Type  = ConsumeFilterType.Regex,
            RegEx = regularExpression
        };

    public static ConsumeFilter FromRegex(ConsumeFilterScope scope, string pattern) =>
        FromRegex(scope, new Regex(pattern, RegexOptions.Compiled));

    public static ConsumeFilter From(ConsumeFilterScope scope, ConsumeFilterType filterType, string expression) {
	    if (IsNullOrWhiteSpace(expression))
		    throw new ArgumentException("Value cannot be null or whitespace.", nameof(expression));

	    return (filterType, scope) switch {
            { filterType: ConsumeFilterType.Prefix }   => FromPrefixes(scope, expression),
            { filterType: ConsumeFilterType.StreamId } => FromStreamId(expression),
            _                                          => FromRegex(scope, expression)
        };
    }

    public static ConsumeFilter ExcludeSystemEvents() =>
        FromRegex(ConsumeFilterScope.Record, new Regex(@"^(?!\$).*", RegexOptions.Compiled));
}

public static class ConsumeFilterExtensions {
	public static IEventFilter ToEventFilter(this ConsumeFilter filter, uint checkpointInterval = 1000) {
		return filter switch {
			{ IsEmptyFilter : true } => EventTypeFilter.None,
			{ IsStreamFilter: true } => StreamFilter.RegularExpression(filter.RegEx, checkpointInterval),
			{ IsRecordFilter: true } => EventTypeFilter.RegularExpression(filter.RegEx, checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid consume filter.")
		};
	}

	public static SubscriptionFilterOptions? ToFilterOptions(this ConsumeFilter filter, uint checkpointInterval = 1000) {
		return filter switch {
			{ IsEmptyFilter : true } => null,
			{ IsStreamFilter: true } => new SubscriptionFilterOptions(StreamFilter.RegularExpression(filter.RegEx, checkpointInterval), checkpointInterval),
			{ IsRecordFilter: true } => new SubscriptionFilterOptions(EventTypeFilter.RegularExpression(filter.RegEx, checkpointInterval), checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid consume filter.")
		};
	}
}
