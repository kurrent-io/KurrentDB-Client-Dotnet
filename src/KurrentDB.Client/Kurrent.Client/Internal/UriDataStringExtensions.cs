namespace Kurrent.Client;

/// <summary>
/// Provides extension methods for URI data string manipulation.
/// </summary>
public static class UriDataStringExtensions {
    /// <summary>
    /// Unescapes a URI data string if it contains percent-encoded characters.
    /// If the string does not contain any '%', it is returned পরিবর্তন না করে.
    /// </summary>
    /// <param name="source">The string to unescape.</param>
    /// <returns>The unescaped string, or the original string if no unescaping was necessary.</returns>
    public static string UnescapeDataStringIfNeeded(this string source) {
        if (source.IndexOf('%') < 0) return source;
        #if NET9_0_OR_GREATER
        Span<char> destination = stackalloc char[source.Length];
        return Uri.TryUnescapeDataString(source, destination, out var charsWritten) ? destination[..charsWritten].ToString() : source;
        #else
        return Uri.UnescapeDataString(source);
        #endif
    }
}
