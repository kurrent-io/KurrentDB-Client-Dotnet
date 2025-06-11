namespace Kurrent.Client;

public static class UriDataStringExtensions {
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
