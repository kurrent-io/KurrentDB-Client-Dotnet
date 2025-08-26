using System.Text.RegularExpressions;

namespace Kurrent.Client.Schema.NameStrategies;

static class SchemaNameExtensions {
	public static string ToSnakeCase(this string input) =>
		Regex.Replace(Regex.Replace(Regex.Replace(input, @"([\p{Lu}]+)([\p{Lu}][\p{Ll}])", "$1_$2"), @"([\p{Ll}\d])([\p{Lu}])", "$1_$2"), @"[-\s]", "_").ToLowerInvariant();

	public static string ToKebabCase(this string input) =>
		ToSnakeCase(input).Replace('_', '-');
}
