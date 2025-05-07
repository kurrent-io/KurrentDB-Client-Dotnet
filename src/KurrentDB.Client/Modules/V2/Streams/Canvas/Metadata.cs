// using System.Globalization;
// using JetBrains.Annotations;
//
// namespace KurrentDB.Client.Model;
//
// public interface IMetadataDecoder {
//     Metadata Decode(ReadOnlyMemory<byte> bytes);
// }
//
// // I need to decode old metadata
// // new metadata is always encoded by us:
// // 1. db supports new contracts - convert to dynamic value map proto
// // 2. db does not support it - use old contracts and serialize to json?
//
// // decoding:
// // 1. db supports new contracts - decode dynamic value map proto
// // 2. db does not support it - decode json
//
//
// // [PublicAPI]
// // public class DefaultMetadataDecoder(JsonSerializerOptions options) : IMetadataDecoder {
// //     public static IMetadataDecoder Instance { get; private set; } = new DefaultMetadataDecoder(new(JsonSerializerDefaults.Web));
// //
// //     public static void SetDefaultSerializer(IMetadataDecoder decoder) => Instance = decoder;
// //
// //     public byte[] EncodeAsJson(Metadata metadata) =>
// // 	    JsonFormatter.Default.Format(metadata.MapToDynamicMapField().ToUtf8JsonBytes());
// //
// //     /// <inheritdoc/>
// //     public Metadata Decode(ReadOnlyMemory<byte> bytes) {
// //         try {
// //             return JsonSerializer.Deserialize<Metadata>(bytes.Span, options) ??
// // 				   throw new MetadataDecodingException(new JsonException("Failed to deserialize metadata"));
// //         } catch (JsonException ex) {
// //             throw new MetadataDecodingException(ex);
// //         }
// //     }
// // }
//
// public class MetadataDecodingException(Exception inner) : Exception("Failed to decode custom metadata", inner);
//
// /// <summary>
// /// Represents a collection of metadata as key-value pairs with additional helper methods.
// /// </summary>
// [PublicAPI]
// public class Metadata : Dictionary<string, object?> {
//     /// <summary>
//     /// Initializes a new, empty instance of the Metadata class using ordinal string comparison.
//     /// </summary>
//     public Metadata()
//         : base(StringComparer.Ordinal) { }
//
//     /// <summary>
//     /// Initializes a new instance of the Metadata class from an existing metadata instance.
//     /// </summary>
//     /// <param name="metadata">The metadata to copy from.</param>
//     public Metadata(Metadata metadata)
//         : base(metadata, StringComparer.Ordinal) { }
//
//     /// <summary>
//     /// Initializes a new instance of the Metadata class from a dictionary.
//     /// </summary>
//     /// <param name="dictionary">The dictionary to copy from.</param>
//     public Metadata(IDictionary<string, object?> dictionary)
//         : base(dictionary, StringComparer.Ordinal) { }
//
//     // /// <summary>
//     // /// Initializes a new instance of the Metadata class from a collection of key-value pairs.
//     // /// </summary>
//     // /// <param name="metadata">The collection to copy from.</param>
//     // public Metadata(IEnumerable<KeyValuePair<string, object?>> metadata)
//     //     : base(metadata, StringComparer.Ordinal) { }
//     //
//     // /// <summary>
//     // /// Initializes a new instance of the Metadata class from a filtered collection of key-value pairs.
//     // /// </summary>
//     // /// <param name="metadata">The collection to copy from.</param>
//     // /// <param name="predicate">The predicate to filter items.</param>
//     // public Metadata(IEnumerable<KeyValuePair<string, object?>> metadata, Predicate<KeyValuePair<string, object?>> predicate)
//     //     : base(metadata.Where(x => predicate(x)), StringComparer.Ordinal) { }
//
//     /// <summary>
//     /// Adds or updates a key-value pair in the metadata and returns the metadata instance.
//     /// </summary>
//     /// <typeparam name="T">The type of the value.</typeparam>
//     /// <param name="key">The key to add or update.</param>
//     /// <param name="value">The value to set.</param>
//     /// <returns>The metadata instance (this) to enable method chaining.</returns>
//     public Metadata Set<T>(string key, T? value) {
//         this[key] = value;
//         return this;
//     }
//
//     public Metadata WithSchemaInfo(SchemaInfo schemaInfo) {
//         schemaInfo.InjectIntoMetadata(this);
//         return this;
//     }
// }
//
//
// [PublicAPI]
// public static class MetadataMapperExtensions {
//
// }
//
// /// <summary>
// /// Extension methods for the Metadata class to provide typed access to metadata values.
// /// </summary>
// [PublicAPI]
// public static class MetadataExtensions {
//     #region . Direct Getters .
//
//     /// <summary>
//     /// Gets a string value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <returns>The string representation of the value or null if the key is not found.</returns>
//     public static string? Get(this Metadata metadata, string key) =>
//         metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
//
//     /// <summary>
//     /// Gets a typed value from the metadata.
//     /// </summary>
//     /// <typeparam name="T">The type to cast the value to.</typeparam>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <returns>The value cast to type T or default(T) if the key is not found or the value cannot be cast.</returns>
//     public static T? Get<T>(this Metadata metadata, string key) =>
//         metadata.TryGetValue(key, out var value) && value is T typedObject ? typedObject : default;
//
//     /// <summary>
//     /// Gets an enum value from the metadata.
//     /// </summary>
//     /// <typeparam name="T">The enum type.</typeparam>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="defaultValue">The default value to return if the key is not found or the value cannot be parsed.</param>
//     /// <returns>The parsed enum value or the default value.</returns>
//     public static T GetEnum<T>(this Metadata metadata, string key, T defaultValue) where T : struct, Enum =>
//         metadata.TryGetValue(key, out var value) && value is not null && Enum.TryParse<T>(value.ToString(), true, out var enumValue)
//             ? enumValue : defaultValue;
//
//     // /// <summary>
//     // /// Gets a numeric value from the metadata.
//     // /// </summary>
//     // /// <typeparam name="T">The numeric type.</typeparam>
//     // /// <param name="metadata">The metadata object.</param>
//     // /// <param name="key">The key to retrieve.</param>
//     // /// <param name="defaultValue">The default value to return if the key is not found or the value cannot be parsed.</param>
//     // /// <returns>The parsed numeric value or the default value.</returns>
//     // public static T GetNumber<T>(this Metadata metadata, string key, T defaultValue) where T : INumber<T> =>
//     //     metadata.TryGetValue(key, out var value) && T.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var numberValue)
//     //         ? numberValue : defaultValue;
//
//     /// <summary>
//     /// Gets a byte array value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <returns>The byte array as ReadOnlyMemory or empty if the key is not found or the value cannot be converted.</returns>
//     public static ReadOnlyMemory<byte> GetBytes(this Metadata metadata, string key) {
//         return metadata.TryGetValue(key, out var value)
//             ? value switch {
//                 null                       => ReadOnlyMemory<byte>.Empty,
//                 byte[] bytes               => bytes,
//                 ReadOnlyMemory<byte> bytes => bytes,
//                 Memory<byte> bytes         => bytes,
//                 _                          => throw new InvalidOperationException($"Cannot convert {value.GetType()} to byte[]")
//             }
//             : ReadOnlyMemory<byte>.Empty;
//     }
//
//     /// <summary>
//     /// Gets a DateTimeOffset value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="defaultValue">The default value to return if the key is not found or the value cannot be parsed.</param>
//     /// <returns>The parsed DateTimeOffset value or the default value.</returns>
//     public static DateTimeOffset GetDateTimeOffset(this Metadata metadata, string key, DateTimeOffset defaultValue) {
//         if (!metadata.TryGetValue(key, out var value) || value is null)
//             return defaultValue;
//
//         if (value is DateTimeOffset dto)
//             return dto;
//
//         if (DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
//             return result;
//
//         return defaultValue;
//     }
//
//     /// <summary>
//     /// Gets a DateTime value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="defaultValue">The default value to return if the key is not found or the value cannot be parsed.</param>
//     /// <returns>The parsed DateTime value or the default value.</returns>
//     public static DateTime GetDateTime(this Metadata metadata, string key, DateTime defaultValue) {
//         if (!metadata.TryGetValue(key, out var value) || value is null)
//             return defaultValue;
//
//         if (value is DateTime dt)
//             return dt;
//
//         if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
//             return result;
//
//         return defaultValue;
//     }
//
//     /// <summary>
//     /// Gets a Boolean value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="defaultValue">The default value to return if the key is not found or the value cannot be parsed.</param>
//     /// <returns>The parsed Boolean value or the default value.</returns>
//     public static bool GetBoolean(this Metadata metadata, string key, bool defaultValue) {
//         if (!metadata.TryGetValue(key, out var value) || value is null)
//             return defaultValue;
//
//         if (value is bool b)
//             return b;
//
//         if (bool.TryParse(value.ToString(), out var result))
//             return result;
//
//         return defaultValue;
//     }
//
//     #endregion
//
//     #region . TryGet Methods .
//
//     /// <summary>
//     /// Tries to get a string value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the value if found; otherwise, null.</param>
//     /// <returns>true if the key was found; otherwise, false.</returns>
//     public static bool TryGet(this Metadata metadata, string key, out string? value) {
//         if (metadata.TryGetValue(key, out var obj)) {
//             value = obj?.ToString();
//             return true;
//         }
//         value = null;
//         return false;
//     }
//
//     // /// <summary>
//     // /// Tries to get a typed value from the metadata.
//     // /// </summary>
//     // /// <typeparam name="T">The type to cast the value to.</typeparam>
//     // /// <param name="metadata">The metadata object.</param>
//     // /// <param name="key">The key to retrieve.</param>
//     // /// <param name="value">When this method returns, contains the value if found and of the correct type; otherwise, default(T).</param>
//     // /// <returns>true if the key was found and the value is of type T; otherwise, false.</returns>
//     // public static bool TryGet<T>(this Metadata metadata, string key, out T? value) {
//     //     if (metadata.TryGetValue(key, out var obj) && obj is T typedValue) {
//     //         value = typedValue;
//     //         return true;
//     //     }
//     //     value = default;
//     //     return false;
//     // }
//
//     /// <summary>
//     /// Tries to get a value of type T from the metadata, with automatic type conversion where appropriate.
//     /// </summary>
//     /// <typeparam name="T">The type to get or convert to.</typeparam>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the value if found and successfully converted; otherwise, default(T).</param>
//     /// <returns>true if the key was found and the value could be converted to type T; otherwise, false.</returns>
//     public static bool TryGet<T>(this Metadata metadata, string key, out T? value) {
// 	    if (!metadata.TryGetValue(key, out var obj)) {
// 		    value = default;
// 		    return false;
// 	    }
//
// 	    // Direct type match
// 	    if (obj is T typedValue) {
// 		    value = typedValue;
// 		    return true;
// 	    }
//
// 	    // Handle null case
// 	    if (obj is null) {
// 		    value = default;
// 		    return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null; // Return true only for nullable types
// 	    }
//
// 	    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
//
// 	    // Handle byte array and memory conversions
// 	    if (targetType == typeof(byte[]) || targetType == typeof(ReadOnlyMemory<byte>) || targetType == typeof(Memory<byte>)) {
// 		    try {
// 			    switch (obj) {
// 				    case byte[] byteArray: {
// 					    if (targetType == typeof(byte[]))
// 						    value = (T)(object)byteArray;
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (T)(object)new ReadOnlyMemory<byte>(byteArray);
// 					    else if (targetType == typeof(Memory<byte>))
// 						    value = (T)(object)new Memory<byte>(byteArray);
// 					    else {
// 						    value = default;
// 						    return false;
// 					    }
//
// 					    return true;
// 				    }
//
// 				    case ReadOnlyMemory<byte> readOnlyMemory: {
// 					    if (targetType == typeof(byte[]))
// 						    value = (T)(object)readOnlyMemory.ToArray();
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (T)(object)readOnlyMemory;
// 					    else if (targetType == typeof(Memory<byte>)) {
// 						    var memoryBytes = readOnlyMemory.ToArray();
// 						    value = (T)(object)new Memory<byte>(memoryBytes);
// 					    }
// 					    else {
// 						    value = default;
// 						    return false;
// 					    }
//
// 					    return true;
// 				    }
//
// 				    case Memory<byte> memory: {
// 					    if (targetType == typeof(byte[]))
// 						    value = (T)(object)memory.ToArray();
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (T)(object)((ReadOnlyMemory<byte>)memory);
// 					    else if (targetType == typeof(Memory<byte>))
// 						    value = (T)(object)memory;
// 					    else {
// 						    value = default;
// 						    return false;
// 					    }
//
// 					    return true;
// 				    }
// 			    }
// 		    }
// 		    catch {
// 			    value = default;
// 			    return false;
// 		    }
// 	    }
//
// 	    // Convert string representation for various types
// 	    var stringValue = obj.ToString();
// 	    if (stringValue == null) {
// 		    value = default;
// 		    return false;
// 	    }
//
// 	    // Handle enum types
// 	    if (targetType.IsEnum) {
// 		    if (Enum.TryParse(targetType, stringValue, true, out var enumValue)) {
// 			    value = (T)enumValue;
// 			    return true;
// 		    }
//
// 		    value = default;
// 		    return false;
// 	    }
//
// 	    // Handle common value types with TryParse methods
// 	    if (targetType == typeof(bool) && bool.TryParse(stringValue, out var boolValue)) {
// 		    value = (T)(object)boolValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(byte) && byte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue)) {
// 		    value = (T)(object)byteValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(sbyte) && sbyte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var sbyteValue)) {
// 		    value = (T)(object)sbyteValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(short) && short.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue)) {
// 		    value = (T)(object)shortValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(ushort) && ushort.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ushortValue)) {
// 		    value = (T)(object)ushortValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(int) && int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue)) {
// 		    value = (T)(object)intValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(uint) && uint.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue)) {
// 		    value = (T)(object)uintValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(long) && long.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue)) {
// 		    value = (T)(object)longValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(ulong) && ulong.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue)) {
// 		    value = (T)(object)ulongValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(float) && float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue)) {
// 		    value = (T)(object)floatValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(double) && double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue)) {
// 		    value = (T)(object)doubleValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(decimal) && decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue)) {
// 		    value = (T)(object)decimalValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(char) && stringValue.Length > 0) {
// 		    value = (T)(object)stringValue[0];
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(Guid) && Guid.TryParse(stringValue, out var guidValue)) {
// 		    value = (T)(object)guidValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanValue)) {
// 		    value = (T)(object)timeSpanValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(DateTime) && DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue)) {
// 		    value = (T)(object)dateTimeValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetValue)) {
// 		    value = (T)(object)dateTimeOffsetValue;
// 		    return true;
// 	    }
//
// 	    #if NET8_0_OR_GREATER
//
// 	    if (targetType == typeof(DateOnly) && DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue)) {
// 		    value = (T)(object)dateOnlyValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(TimeOnly) && TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue)) {
// 		    value = (T)(object)timeOnlyValue;
// 		    return true;
// 	    }
//
// 		#endif
//
// 	    if (targetType == typeof(Uri) && Uri.TryCreate(stringValue, UriKind.RelativeOrAbsolute, out var uriValue)) {
// 		    value = (T)(object)uriValue;
// 		    return true;
// 	    }
//
// 	    // If we couldn't convert, return default
// 	    value = default;
// 	    return false;
//     }
//
//     /// <summary>
//     /// Tries to get an enum value from the metadata.
//     /// </summary>
//     /// <typeparam name="T">The enum type.</typeparam>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the parsed enum value if successful; otherwise, default(T).</param>
//     /// <returns>true if the key was found and the value could be parsed as the enum type; otherwise, false.</returns>
//     public static bool TryGetEnum<T>(this Metadata metadata, string key, out T value) where T : struct, Enum {
//         if (metadata.TryGetValue(key, out var obj) && obj is not null) {
//             if (obj is T enumValue) {
//                 value = enumValue;
//                 return true;
//             }
//
//             if (Enum.TryParse<T>(obj.ToString(), true, out var parsedValue)) {
//                 value = parsedValue;
//                 return true;
//             }
//         }
//         value = default;
//         return false;
//     }
//
//     // /// <summary>
//     // /// Tries to get a numeric value from the metadata.
//     // /// </summary>
//     // /// <typeparam name="T">The numeric type.</typeparam>
//     // /// <param name="metadata">The metadata object.</param>
//     // /// <param name="key">The key to retrieve.</param>
//     // /// <param name="value">When this method returns, contains the parsed numeric value if successful; otherwise, 0.</param>
//     // /// <returns>true if the key was found and the value could be parsed as the numeric type; otherwise, false.</returns>
//     // public static bool TryGetNumber<T>(this Metadata metadata, string key, out T value) where T : INumber<T> {
//     //     if (metadata.TryGetValue(key, out var obj) && obj is not null) {
//     //         if (obj is T number) {
//     //             value = number;
//     //             return true;
//     //         }
//     //
//     //         if (T.TryParse(obj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue)) {
//     //             value = parsedValue;
//     //             return true;
//     //         }
//     //     }
//     //     value = T.Zero;
//     //     return false;
//     // }
//
//     /// <summary>
//     /// Tries to get a byte array value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the byte array if successful; otherwise, empty.</param>
//     /// <returns>true if the key was found and the value could be converted to a byte array; otherwise, false.</returns>
//     public static bool TryGetBytes(this Metadata metadata, string key, out ReadOnlyMemory<byte> value) {
//         if (metadata.TryGetValue(key, out var obj)) {
//             try {
//                 value = obj switch {
//                     null => ReadOnlyMemory<byte>.Empty,
//                     byte[] bytes => bytes,
//                     ReadOnlyMemory<byte> bytes => bytes,
//                     Memory<byte> bytes => bytes,
//                     _ => ReadOnlyMemory<byte>.Empty
//                 };
//                 return obj is not null;
//             }
//             catch {
//                 value = ReadOnlyMemory<byte>.Empty;
//                 return false;
//             }
//         }
//         value = ReadOnlyMemory<byte>.Empty;
//         return false;
//     }
//
//     /// <summary>
//     /// Tries to get a DateTimeOffset value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the parsed DateTimeOffset value if successful; otherwise, default.</param>
//     /// <returns>true if the key was found and the value could be parsed as DateTimeOffset; otherwise, false.</returns>
//     public static bool TryGetDateTimeOffset(this Metadata metadata, string key, out DateTimeOffset value) {
//         if (metadata.TryGetValue(key, out var obj)) {
//             if (obj is DateTimeOffset dto) {
//                 value = dto;
//                 return true;
//             }
//
//             if (obj is not null && DateTimeOffset.TryParse(obj.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) {
//                 value = result;
//                 return true;
//             }
//         }
//         value = default;
//         return false;
//     }
//
//     /// <summary>
//     /// Tries to get a DateTime value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the parsed DateTime value if successful; otherwise, default.</param>
//     /// <returns>true if the key was found and the value could be parsed as DateTime; otherwise, false.</returns>
//     public static bool TryGetDateTime(this Metadata metadata, string key, out DateTime value) {
//         if (metadata.TryGetValue(key, out var obj)) {
//             if (obj is DateTime dt) {
//                 value = dt;
//                 return true;
//             }
//
//             if (obj is not null && DateTime.TryParse(obj.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)) {
//                 value = result;
//                 return true;
//             }
//         }
//         value = default;
//         return false;
//     }
//
//     /// <summary>
//     /// Tries to get a Boolean value from the metadata.
//     /// </summary>
//     /// <param name="metadata">The metadata object.</param>
//     /// <param name="key">The key to retrieve.</param>
//     /// <param name="value">When this method returns, contains the parsed Boolean value if successful; otherwise, default.</param>
//     /// <returns>true if the key was found and the value could be parsed as Boolean; otherwise, false.</returns>
//     public static bool TryGetBoolean(this Metadata metadata, string key, out bool value) {
//         if (metadata.TryGetValue(key, out var obj)) {
//             if (obj is bool b) {
//                 value = b;
//                 return true;
//             }
//
//             if (obj is not null && bool.TryParse(obj.ToString(), out var result)) {
//                 value = result;
//                 return true;
//             }
//         }
//         value = false;
//         return false;
//     }
//
//     #endregion
// }
