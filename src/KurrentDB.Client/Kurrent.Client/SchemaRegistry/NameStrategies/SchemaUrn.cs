using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a schema identifier using the URN format: urn:schemas-kurrent:{namespace}:{messageName}[:{guid}]
/// </summary>
/// <remarks>
/// This implementation ensures full reversibility between URN strings and their component parts.
/// The namespace portion may be a CLR namespace or any other namespace identifier.
/// </remarks>
public partial record SchemaUrn {
	public const string UrnPrefix = "urn:schemas-kurrent:";

	const char Separator = ':';

	const string Pattern = @"^urn\:schemas\-kurrent\:([^:]+):([^:]+)(?::([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}))?$";

	// Pattern to match: urn:schemas-kurrent:<namespace>:<messageName>[:<guid>]
	[GeneratedRegex(Pattern , RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex GetUrnRegex();

	// Using GeneratedRegex for performance in parsing operations
	static readonly Regex UrnRegex = GetUrnRegex();

	/// <summary>
	/// Gets the namespace identifier for this schema.
	/// </summary>
	/// <remarks>
	/// This may be a CLR namespace (e.g., "MyCompany.Product") or any other namespace identifier.
	/// </remarks>
	public string Namespace { get; private init; } = "";

	/// <summary>
	/// Gets the name of the message.
	/// </summary>
	public string MessageName { get; private init; } = "";

	/// <summary>
	/// Gets the GUID of the schema, if specified.
	/// </summary>
	/// <remarks>
	/// A null value indicates that no GUID was specified.
	/// </remarks>
	public Guid? SchemaGuid { get; private init; }

	/// <summary>
	/// Gets the full URN string representation.
	/// </summary>
	public string Value { get; private init; } = "";

	/// <summary>
	/// Creates a new SchemaUrn.
	/// </summary>
	/// <param name="ns">The namespace identifier (CLR namespace or other prefix).</param>
	/// <param name="messageName">The name of the message.</param>
	/// <param name="schemaVersionId">Optional GUID to uniquely identify this schema.</param>
	/// <returns>A new SchemaUrn instance.</returns>
	/// <exception cref="ArgumentException">Thrown if namespace or messageName is null, empty, or contains invalid characters.</exception>
	public static SchemaUrn Create(string ns, string messageName, Guid? schemaVersionId = null) {
		if (string.IsNullOrWhiteSpace(ns))
			throw new ArgumentException("Namespace cannot be empty, or whitespace", nameof(ns));

		if (ns.Contains(Separator))
			throw new ArgumentException($"Namespace cannot contain the '{Separator}' character", nameof(ns));

		if (string.IsNullOrWhiteSpace(messageName))
			throw new ArgumentException("Message name cannot be empty, or whitespace", nameof(messageName));

		if (messageName.Contains(Separator))
			throw new ArgumentException($"Message name cannot contain the '{Separator}' character", nameof(messageName));

		var urn = $"{UrnPrefix}{ns}{Separator}{messageName}";

		if (schemaVersionId.HasValue)
			urn += $"{Separator}{schemaVersionId.Value:D}";

		return new SchemaUrn {
			Namespace   = ns,
			MessageName = messageName,
			SchemaGuid  = schemaVersionId,
			Value       = urn
		};
	}

	/// <summary>
	/// Attempts to parse a string as a SchemaUrn.
	/// </summary>
	/// <param name="input">The URN string to parse.</param>
	/// <param name="urn">The parsed SchemaUrn if successful.</param>
	/// <returns>True if parsing was successful; otherwise, false.</returns>
	public static bool TryParse(string input, [MaybeNullWhen(false)] out SchemaUrn urn) {
		urn = null;

		if (string.IsNullOrEmpty(input))
			return false;

		var match = UrnRegex.Match(input);

		if (!match.Success)
			return false;

		var ns          = match.Groups[1].Value;
		var messageName = match.Groups[2].Value;

		try {
			urn = match.Groups[3].Success && Guid.TryParse(match.Groups[3].ValueSpan, out var schemaVersionId)
				? Create(ns, messageName, schemaVersionId)
				: Create(ns, messageName);

			return true;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Parses a string as a SchemaUrn.
	/// </summary>
	/// <param name="urn">The URN string to parse.</param>
	/// <returns>A new SchemaUrn instance.</returns>
	/// <exception cref="ArgumentException">Thrown if the input string is null or empty.</exception>
	/// <exception cref="FormatException">Thrown if the string is not a valid SchemaUrn.</exception>
	public static SchemaUrn Parse(string urn) {
		if (string.IsNullOrWhiteSpace(urn))
			throw new ArgumentException("URN cannot be empty, or whitespace", nameof(urn));

		if (TryParse(urn, out var result))
			return result;

		throw new FormatException(
			$"Invalid schema URN format: '{urn}'. " +
			$"Expected format: {UrnPrefix}{{namespace}}:{{messageName}}[:{{{Guid.Empty}}}]"
		);
	}

	/// <summary>
	/// Returns a string representation of this SchemaUrn.
	/// </summary>
	/// <returns>The full URN string.</returns>
	public override string ToString() => Value;

	/// <summary>
	/// Implicit conversion from SchemaUrn to string.
	/// </summary>
	/// <param name="urn">The SchemaUrn to convert.</param>
	/// <returns>The string representation of the SchemaUrn.</returns>
	public static implicit operator string(SchemaUrn urn) => urn.Value;
}
