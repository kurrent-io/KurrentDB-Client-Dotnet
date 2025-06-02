// using Humanizer;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a Uniform Resource Name (URN) composed of a Namespace Identifier (NID)
/// and a Namespace Specific String (NSS). Provides functionality to generate, validate,
/// and manipulate URNs.
/// </summary>
/// <param name="Nid">The Namespace Identifier, representing a specific naming scope.</param>
/// <param name="Nss">The Namespace Specific String, representing the unique name within the namespace scope.</param>
[PublicAPI]
public record SchemaNameUrn(string Nid, string Nss) {
	public string Value { get; } = $"urn:{Nid}:{Nss}";

	public override string ToString() => Value;

	public static implicit operator string(SchemaNameUrn urn) => urn.Value;

	/// <summary>
	/// Generates a formatted URN using the provided Namespace Identifier (NID) and Namespace Specific String (NSS).
	/// </summary>
	/// <param name="nid">The Namespace Identifier, representing the naming scope (e.g., "kurrent.io:users").</param>
	/// <param name="nss">The Namespace Specific String, representing the specific name (e.g., "UserCreated").</param>
	/// <param name="preserveDots">Indicates whether to preserve dots in the NID and NSS.</param>
	/// <returns>A valid URN with the specified NID and NSS.</returns>
	/// <exception cref="ArgumentException">Thrown when either the NID or NSS is null, empty, or consists only of white spaces.</exception>
	public static SchemaNameUrn Create(ReadOnlySpan<char> nid, ReadOnlySpan<char> nss, bool preserveDots = true) {
		if (nid.IsEmpty || nid.IsWhiteSpace())
			throw new ArgumentException("Namespace Identifier cannot be empty or white space", nameof(nid));

		if (nss.IsEmpty || nss.IsWhiteSpace())
			throw new ArgumentException("Namespace Specific String cannot be empty or white space", nameof(nss));

		return new SchemaNameUrn(Normalize(nid, preserveDots), Normalize(nss, false));

		static string Normalize(ReadOnlySpan<char> value, bool preserveDots) {
			var result = value
				.Trim().ToString()
				.Replace('\\', ':')
				.Replace('/', ':');

			if (!preserveDots)
				result = result.Replace('.', ':');

			return result
				.ToSnakeCase()
				.ToLowerInvariant();
		}
	}
}
