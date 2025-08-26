using System.Globalization;
using System.Text.Json;

namespace Kurrent.Client;

static class Utf8JsonReaderExtensions {
    public static bool TryGetTimeSpan(this ref Utf8JsonReader reader, out TimeSpan value) {
        if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), CultureInfo.InvariantCulture, out value))
            return true;

        value = TimeSpan.Zero;
        return false;
    }
}
