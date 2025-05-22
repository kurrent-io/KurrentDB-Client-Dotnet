using System.Text.RegularExpressions;
using static System.String;

namespace KurrentDB.Client.Model;

public enum ConsumeFilterScope {
	Unspecified = 0,
	Stream      = 1,
	Record      = 2
}

public enum ConsumeFilterType {
	Unspecified = 0,
	Literal     = 1,
	Regex       = 2
}

[PublicAPI]
public record ConsumeFilter {
	public static readonly ConsumeFilter None = new();

	public ConsumeFilterScope Scope      { get; private init; } = ConsumeFilterScope.Unspecified;
	public ConsumeFilterType  Type       { get; private init; } = ConsumeFilterType.Unspecified;
	public string             Expression { get; private init; } = Empty;

	Regex Regex { get; init; } = new(Empty);

	public override string ToString() => $"[{Scope}|{Type}] {Expression}";

	public bool IsLiteralFilter  => Type == ConsumeFilterType.Literal;
	public bool IsRegexFilter    => Type == ConsumeFilterType.Regex;
	public bool IsStreamFilter   => Scope is ConsumeFilterScope.Stream;
	public bool IsRecordFilter   => Scope is ConsumeFilterScope.Record;

	public bool IsStreamNameFilter => Type == ConsumeFilterType.Literal && Scope == ConsumeFilterScope.Stream;
	public bool IsEmptyFilter      => Type == ConsumeFilterType.Unspecified && Scope == ConsumeFilterScope.Unspecified;

	public bool IsMatch(ReadOnlySpan<char> input) => Regex.IsMatch(input);

    public static ConsumeFilter FromStream(string stream) {
	    if (IsNullOrWhiteSpace(stream))
		    throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));

	    if (stream.StartsWith("~"))
		    throw new ArgumentException("Stream name cannot start with '~'.", nameof(stream));

	    if (stream.Length < 2)
		    throw new ArgumentException("Stream name must be at least 2 characters long.", nameof(stream));

	    return new() {
		    Scope = ConsumeFilterScope.Stream,
		    Type  = ConsumeFilterType.Literal,
		    Regex = new Regex(stream)
	    };
    }

    public static ConsumeFilter FromPrefixes(ConsumeFilterScope scope, params string[] prefixes) {
	    if (prefixes.Length == 0)
		    throw new ArgumentException("Prefixes cannot be empty.", nameof(prefixes));

	    return new ConsumeFilter {
            Scope = scope,
            Type  = ConsumeFilterType.Regex,
            Regex = new Regex(CreatePattern(prefixes))
        };

        // Escape special characters in the prefixes and join them with '|'
        // Add '^' at the start to match the start of the string
        static string CreatePattern(string[] prefixes) {
	        var escapedPrefixes = prefixes
		        .Select(prefix => {
			        if (IsNullOrWhiteSpace(prefix))
				        throw new ArgumentException("Prefix cannot be empty.", nameof(prefixes));

			        return Regex.Escape(prefix);
		        });

	        return $"^({Join("|", escapedPrefixes)})";
        }
    }

    public static ConsumeFilter FromPrefixes(ConsumeFilterScope scope, string expression) {
	    if (IsNullOrWhiteSpace(expression))
		    throw new ArgumentException("Prefix expression cannot be empty.", nameof(expression));

	    return FromPrefixes(scope, expression.Split([','], StringSplitOptions.RemoveEmptyEntries));
    }

    public static ConsumeFilter FromRegex(ConsumeFilterScope scope, string pattern) {
	    var expression = pattern.StartsWith("~") ? pattern[1..] : pattern;

	    try {
		    return new ConsumeFilter {
			    Scope      = scope,
			    Type       = ConsumeFilterType.Regex,
			    Expression = expression,
			    Regex      = new Regex(expression, RegexOptions.Compiled)
		    };
	    }
	    catch (ArgumentException ex) {
		    throw new ArgumentException($"Invalid regex pattern: {pattern}", nameof(pattern), ex);
	    }
    }

    public static ConsumeFilter Create(ConsumeFilterScope scope, string expression) {
	    if (IsNullOrWhiteSpace(expression))
		    throw new ArgumentNullException(nameof(expression));

	    return expression.StartsWith("~")
		    ? FromRegex(scope, expression)
		    : new ConsumeFilter {
			    Scope      = scope,
			    Type       = ConsumeFilterType.Literal,
			    Expression = expression,
			    Regex      = new Regex(expression, RegexOptions.Compiled)
		    };
    }
}

public static class ConsumeFilterExtensions {
	public static IEventFilter ToEventFilter(this ConsumeFilter filter, uint checkpointInterval = 1000) {
		return filter switch {
			{ IsEmptyFilter : true } => EventTypeFilter.None,
			{ IsStreamFilter: true } => StreamFilter.RegularExpression(filter.Expression, checkpointInterval),
			{ IsRecordFilter: true } => EventTypeFilter.RegularExpression(filter.Expression, checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid consume filter.")
		};
	}

	public static SubscriptionFilterOptions? ToFilterOptions(this ConsumeFilter filter, uint checkpointInterval = 1000) {
		return filter switch {
			{ IsEmptyFilter : true } => null,
			{ IsStreamFilter: true } => new SubscriptionFilterOptions(StreamFilter.RegularExpression(filter.Expression, checkpointInterval), checkpointInterval),
			{ IsRecordFilter: true } => new SubscriptionFilterOptions(EventTypeFilter.RegularExpression(filter.Expression, checkpointInterval), checkpointInterval),
			_                        => throw new ArgumentOutOfRangeException(nameof(filter), "Invalid consume filter.")
		};
	}
}
