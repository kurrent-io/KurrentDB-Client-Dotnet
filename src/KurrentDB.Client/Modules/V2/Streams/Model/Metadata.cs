using System.Globalization;
using KurrentDB.Client.SchemaRegistry;

namespace KurrentDB.Client.Model;

/// <summary>
/// Provides functionality to decode metadata from serialized formats.
/// </summary>
public interface IMetadataDecoder {
    Metadata Decode(ReadOnlyMemory<byte> bytes);
}

// I need to decode old metadata
// new metadata is always encoded by us:
// 1. db supports new contracts - convert to dynamic value map proto
// 2. db does not support it - use old contracts and serialize to json?

// decoding:
// 1. db supports new contracts - decode dynamic value map proto
// 2. db does not support it - decode json


// [PublicAPI]
// public class DefaultMetadataDecoder : IMetadataDecoder {
//     public static IMetadataDecoder Instance { get; private set; } = new DefaultMetadataDecoder();
//
//     public static void SetDefaultSerializer(IMetadataDecoder decoder) => Instance = decoder;
//
//     public byte[] EncodeAsJson(Metadata metadata) =>
// 	    JsonFormatter.Default.Format(metadata.MapToDynamicMapField().ToUtf8JsonBytes());
//
//     /// <inheritdoc/>
//     public Metadata Decode(ReadOnlyMemory<byte> bytes) {
//         try {
//             return JsonSerializer.Deserialize<Metadata>(bytes.Span, options) ??
// 				   throw new MetadataDecodingException(new JsonException("Failed to deserialize metadata"));
//         } catch (JsonException ex) {
//             throw new MetadataDecodingException(ex);
//         }
//     }
// }

public class MetadataDecodingException(Exception inner) : Exception("Failed to decode custom metadata", inner);

/// <summary>
/// Represents a collection of metadata as key-value pairs with additional helper methods.
/// </summary>
[PublicAPI]
public class Metadata : Dictionary<string, object?> {
    /// <summary>
    /// Initializes a new, empty instance of the Metadata class using ordinal string comparison.
    /// </summary>
    public Metadata()
        : base(StringComparer.Ordinal) { }

    /// <summary>
    /// Initializes a new instance of the Metadata class from an existing metadata instance.
    /// </summary>
    /// <param name="metadata">The metadata to copy from.</param>
    public Metadata(Metadata metadata)
        : base(metadata, StringComparer.Ordinal) { }

    /// <summary>
    /// Initializes a new instance of the Metadata class from a dictionary.
    /// </summary>
    /// <param name="dictionary">The dictionary to copy from.</param>
    public Metadata(IDictionary<string, object?> dictionary)
        : base(dictionary, StringComparer.Ordinal) { }

    // /// <summary>
    // /// Initializes a new instance of the Metadata class from a collection of key-value pairs.
    // /// </summary>
    // /// <param name="metadata">The collection to copy from.</param>
    // public Metadata(IEnumerable<KeyValuePair<string, object?>> metadata)
    //     : base(metadata, StringComparer.Ordinal) { }
    //
    // /// <summary>
    // /// Initializes a new instance of the Metadata class from a filtered collection of key-value pairs.
    // /// </summary>
    // /// <param name="metadata">The collection to copy from.</param>
    // /// <param name="predicate">The predicate to filter items.</param>
    // public Metadata(IEnumerable<KeyValuePair<string, object?>> metadata, Predicate<KeyValuePair<string, object?>> predicate)
    //     : base(metadata.Where(x => predicate(x)), StringComparer.Ordinal) { }

    /// <summary>
    /// Adds or updates a key-value pair in the metadata and returns the metadata instance.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The metadata instance (this) to enable method chaining.</returns>
    public Metadata Set<T>(string key, T? value) {
        this[key] = value;
        return this;
    }

    public Metadata TrySet<T>(string key, T? value) {
	    if (!ContainsKey(key))
		    this[key] = value;

	    return this;
    }
}

/// <summary>
/// Extension methods for the Metadata class to provide typed access to metadata values.
/// </summary>
[PublicAPI]
public static partial class MetadataExtensions {
	/// <summary>
	/// Gets a typed value from the metadata.
	/// </summary>
	/// <typeparam name="T">The type to cast the value to.</typeparam>
	/// <param name="metadata">The metadata object.</param>
	/// <param name="key">The key to retrieve.</param>
	public static T? Get<T>(this Metadata metadata, string key) =>
		metadata.TryGet<T>(key, out var value)  ? value : default;

	/// <summary>
	/// Gets a typed value from the metadata, with automatic type conversion where appropriate.
	/// </summary>
	/// <typeparam name="T">The type to cast the value to.</typeparam>
	/// <param name="metadata">The metadata object.</param>
	/// <param name="key">The key to retrieve.</param>
	/// <param name="defaultValue">The default value to return if the key is not found or the value can't be cast to type T.</param>
	public static T? Get<T>(this Metadata metadata, string key, T defaultValue) =>
		metadata.TryGetValue(key, out var value) && value is T typedObject ? typedObject : defaultValue;

    /// <summary>
    /// Tries to get a typed value from the metadata, with automatic type conversion where appropriate.
    /// </summary>
    /// <typeparam name="T">The type to get or convert to.</typeparam>
    /// <param name="metadata">The metadata object.</param>
    /// <param name="key">The key to retrieve.</param>
    /// <param name="value">When this method returns, contains the value if found and successfully converted; otherwise, default(T).</param>
    /// <returns>true if the key was found and the value could be converted to type T; otherwise, false.</returns>
    public static bool TryGet<T>(this Metadata metadata, string key, out T? value) {
	    if (!metadata.TryGetValue(key, out var obj)) {
		    value = default;
		    return false;
	    }

	    // Direct type match
	    if (obj is T typedValue) {
		    value = typedValue;
		    return true;
	    }

	    // Handle null case
	    if (obj is null) {
		    value = default;
		    return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null; // Return true only for nullable types
	    }

	    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

	    // Handle byte array and memory conversions
	    if (targetType == typeof(byte[]) || targetType == typeof(ReadOnlyMemory<byte>) || targetType == typeof(Memory<byte>)) {
		    try {
			    switch (obj) {
				    case byte[] byteArray: {
					    if (targetType == typeof(byte[]))
						    value = (T)(object)byteArray;
					    else if (targetType == typeof(ReadOnlyMemory<byte>))
						    value = (T)(object)new ReadOnlyMemory<byte>(byteArray);
					    else if (targetType == typeof(Memory<byte>))
						    value = (T)(object)new Memory<byte>(byteArray);
					    else {
						    value = default;
						    return false;
					    }

					    return true;
				    }

				    case ReadOnlyMemory<byte> readOnlyMemory: {
					    if (targetType == typeof(byte[]))
						    value = (T)(object)readOnlyMemory.ToArray();
					    else if (targetType == typeof(ReadOnlyMemory<byte>))
						    value = (T)(object)readOnlyMemory;
					    else if (targetType == typeof(Memory<byte>)) {
						    var memoryBytes = readOnlyMemory.ToArray();
						    value = (T)(object)new Memory<byte>(memoryBytes);
					    }
					    else {
						    value = default;
						    return false;
					    }

					    return true;
				    }

				    case Memory<byte> memory: {
					    if (targetType == typeof(byte[]))
						    value = (T)(object)memory.ToArray();
					    else if (targetType == typeof(ReadOnlyMemory<byte>))
						    value = (T)(object)((ReadOnlyMemory<byte>)memory);
					    else if (targetType == typeof(Memory<byte>))
						    value = (T)(object)memory;
					    else {
						    value = default;
						    return false;
					    }

					    return true;
				    }
			    }
		    }
		    catch {
			    value = default;
			    return false;
		    }
	    }

	    // Convert string representation for various types
	    var stringValue = obj.ToString();
	    if (stringValue == null) {
		    value = default;
		    return false;
	    }

	    // // Handle enum types
	    // if (targetType.IsEnum) {
		   //  if (Enum.TryParse(targetType, stringValue, true, out var enumValue)) {
			  //   value = (T)enumValue;
			  //   return true;
		   //  }
	    //
		   //  value = default;
		   //  return false;
	    // }

	    // Handle common value types with TryParse methods
	    if (targetType == typeof(bool) && bool.TryParse(stringValue, out var boolValue)) {
		    value = (T)(object)boolValue;
		    return true;
	    }

	    if (targetType == typeof(byte) && byte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue)) {
		    value = (T)(object)byteValue;
		    return true;
	    }

	    if (targetType == typeof(sbyte) && sbyte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var sbyteValue)) {
		    value = (T)(object)sbyteValue;
		    return true;
	    }

	    if (targetType == typeof(short) && short.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue)) {
		    value = (T)(object)shortValue;
		    return true;
	    }

	    if (targetType == typeof(ushort) && ushort.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ushortValue)) {
		    value = (T)(object)ushortValue;
		    return true;
	    }

	    if (targetType == typeof(int) && int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue)) {
		    value = (T)(object)intValue;
		    return true;
	    }

	    if (targetType == typeof(uint) && uint.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue)) {
		    value = (T)(object)uintValue;
		    return true;
	    }

	    if (targetType == typeof(long) && long.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue)) {
		    value = (T)(object)longValue;
		    return true;
	    }

	    if (targetType == typeof(ulong) && ulong.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue)) {
		    value = (T)(object)ulongValue;
		    return true;
	    }

	    if (targetType == typeof(float) && float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue)) {
		    value = (T)(object)floatValue;
		    return true;
	    }

	    if (targetType == typeof(double) && double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue)) {
		    value = (T)(object)doubleValue;
		    return true;
	    }

	    if (targetType == typeof(decimal) && decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue)) {
		    value = (T)(object)decimalValue;
		    return true;
	    }

	    if (targetType == typeof(char) && stringValue.Length > 0) {
		    value = (T)(object)stringValue[0];
		    return true;
	    }

	    if (targetType == typeof(Guid) && Guid.TryParse(stringValue, out var guidValue)) {
		    value = (T)(object)guidValue;
		    return true;
	    }

	    if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanValue)) {
		    value = (T)(object)timeSpanValue;
		    return true;
	    }

	    if (targetType == typeof(DateTime) && DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue)) {
		    value = (T)(object)dateTimeValue;
		    return true;
	    }

	    if (targetType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetValue)) {
		    value = (T)(object)dateTimeOffsetValue;
		    return true;
	    }

	    if (targetType == typeof(DateOnly) && DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue)) {
		    value = (T)(object)dateOnlyValue;
		    return true;
	    }

	    if (targetType == typeof(TimeOnly) && TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue)) {
		    value = (T)(object)timeOnlyValue;
		    return true;
	    }

	    if (targetType == typeof(Uri) && Uri.TryCreate(stringValue, UriKind.RelativeOrAbsolute, out var uriValue)) {
		    value = (T)(object)uriValue;
		    return true;
	    }

	    // If we couldn't convert, return default
	    value = default;
	    return false;
    }
}

public static partial class MetadataExtensions {
	public static RecordSchemaInfo GetSchemaInfo(this Metadata metadata) =>
		new(GetSchemaName(metadata), GetSchemaDataFormat(metadata), GetSchemaVersionId(metadata));

	public static SchemaName GetSchemaName(this Metadata metadata) =>
		metadata.TryGetSchemaName(out var schemaName) ? schemaName : SchemaName.None;

	public static SchemaDataFormat GetSchemaDataFormat(this Metadata metadata) =>
		metadata.Get(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Unspecified);

	public static SchemaVersionId GetSchemaVersionId(this Metadata metadata) =>
		metadata.TryGet<string>(SystemMetadataKeys.SchemaVersionId, out var schemaVersionId) ? SchemaVersionId.From(schemaVersionId!) : SchemaVersionId.None;

	public static bool TryGetSchemaName(this Metadata metadata, out SchemaName schemaName) {
		if (metadata.TryGet<string>(SystemMetadataKeys.SchemaName, out var value)) {
			schemaName = SchemaName.From(value!);
			return true;
		}

		schemaName = SchemaName.None;
		return false;
	}
}
