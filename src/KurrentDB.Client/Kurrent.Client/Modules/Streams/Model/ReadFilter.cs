using System.Text.RegularExpressions;
using static System.String;

namespace Kurrent.Client.Streams;

/// <summary>
/// Represents a filter used to match or restrict data in a reading operation.
/// The filter can be applied to streams or records and supports literal matching
/// or regular expression-based matching.
/// </summary>
[PublicAPI]
public record ReadFilter {
	public static readonly ReadFilter None = new();

	const char RegexIndicator = '~';

	public ReadFilterScope Scope      { get; private init; } = ReadFilterScope.Unspecified;
	public ReadFilterType  Type       { get; private init; } = ReadFilterType.Unspecified;
	public string          Expression { get; private init; } = Empty;

	Regex Regex { get; init; } = new(Empty);

	public override string ToString() => $"[{Scope}|{Type}] {Expression}";

	public bool IsLiteralFilter => Type == ReadFilterType.Literal;
	public bool IsRegexFilter   => Type == ReadFilterType.Regex;
	public bool IsStreamFilter  => Scope is ReadFilterScope.Stream;
	public bool IsRecordFilter  => Scope is ReadFilterScope.Record;

	public bool IsStreamNameFilter => Type == ReadFilterType.Literal && Scope == ReadFilterScope.Stream;
	public bool IsEmptyFilter      => Type == ReadFilterType.Unspecified && Scope == ReadFilterScope.Unspecified;

	public bool IsMatch(ReadOnlySpan<char> input) => Regex.IsMatch(input);

    public static ReadFilter FromStream(string stream) {
	    if (IsNullOrWhiteSpace(stream))
		    throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));

	    if (stream.StartsWith(RegexIndicator))
		    throw new ArgumentException("Stream name cannot start with '~' because it is reserved for regex patterns.", nameof(stream));

	    if (stream.Length < 2)
		    throw new ArgumentException("Stream name must be at least 2 characters long.", nameof(stream));

	    return new() {
		    Scope = ReadFilterScope.Stream,
		    Type  = ReadFilterType.Literal,
		    Regex = new Regex(stream)
	    };
    }

    public static ReadFilter FromPrefixes(ReadFilterScope scope, params string[] prefixes) {
	    if (prefixes.Length == 0)
		    throw new ArgumentException("Prefixes cannot be empty.", nameof(prefixes));

	    var expression = CreatePattern(prefixes);

	    return new ReadFilter {
            Scope      = scope,
            Type       = ReadFilterType.Regex,
            Expression = expression,
            Regex      = new Regex(expression)
        };

        // Escape special characters in the prefixes and join them with '|'
        // Add '^' at the start to match the start of the string
        static string CreatePattern(string[] prefixes) {
	        var escapedPrefixes = prefixes
		        .Select(prefix => IsNullOrWhiteSpace(prefix)
			        ? throw new ArgumentException("Prefix cannot be empty.", nameof(prefixes))
			        : Regex.Escape(prefix));

	        return $"^({Join("|", escapedPrefixes)})";
        }
    }

    public static ReadFilter FromPrefixes(ReadFilterScope scope, string expression) {
	    if (IsNullOrWhiteSpace(expression))
		    throw new ArgumentException("Prefix expression cannot be empty.", nameof(expression));

	    return FromPrefixes(scope, expression.Split([','], StringSplitOptions.RemoveEmptyEntries));
    }

    public static ReadFilter FromRegex(ReadFilterScope scope, string pattern) {
	    var expression = pattern.StartsWith(RegexIndicator) ? pattern[1..] : pattern;

	    try {
		    return new ReadFilter {
			    Scope      = scope,
			    Type       = ReadFilterType.Regex,
			    Expression = expression,
			    Regex      = new Regex(expression, RegexOptions.Compiled)
		    };
	    }
	    catch (Exception ex) {
		    throw new ArgumentException($"Invalid regex pattern: {pattern}", nameof(pattern), ex);
	    }
    }

    public static ReadFilter Create(ReadFilterScope scope, string expression) {
	    if (IsNullOrWhiteSpace(expression))
		    throw new ArgumentNullException(nameof(expression));

	    return expression.StartsWith(RegexIndicator)
		    ? FromRegex(scope, expression)
		    : new ReadFilter {
			    Scope      = scope,
			    Type       = ReadFilterType.Literal,
			    Expression = expression,
			    Regex      = new Regex(expression, RegexOptions.Compiled)
		    };
    }
}

public enum ReadFilterScope {
	Unspecified = 0,

	/// <summary>
	/// The filter will be applied to the record stream name
	/// </summary>
	Stream = 1,

	/// <summary>
	/// The filter will be applied to the record schema name
	/// </summary>
	SchemaName = 2,

	/// <summary>
	/// The filter will be applied to the properties of the record
	/// </summary>
	Properties = 3,

	/// <summary>
	/// The filter will be applied to all the record properties
	/// including the stream and schema name
	/// </summary>
	Record = 4
}

public enum ReadFilterType {
	Unspecified = 0,
	Literal     = 1,
	Regex       = 2
}
