using System.Globalization;
using System.Text.Json;
using System.Xml;

namespace KurrentDB.Client.Core.Internal.Exceptions;

static class Utf8JsonReaderExtensions {
	public static bool TryGetTimeSpan(this ref Utf8JsonReader reader, out TimeSpan value) {
		if (reader.TokenType == JsonTokenType.String) {
			var str = reader.GetString();
			if (TimeSpan.TryParse(str, CultureInfo.InvariantCulture, out value))
				return true;

			try {
				value = XmlConvert.ToTimeSpan(str!);
				return true;
			} catch (FormatException) {
				// ignore
			}
		}

		value = TimeSpan.Zero;
		return false;
	}
}
