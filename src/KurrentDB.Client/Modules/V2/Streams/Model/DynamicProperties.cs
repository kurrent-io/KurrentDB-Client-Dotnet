// using System.Collections;
// using System.Diagnostics;
// using System.Globalization;
//
// namespace KurrentDB.Client.Model;
//
// /// <summary>
// /// Represents the key used to get and set <see cref="DynamicDictionary"/> values.
// /// </summary>
// /// <typeparam name="TValue">The value type.</typeparam>
// public readonly struct DynamicDictionaryKey<TValue> {
// 	/// <summary>
// 	/// Gets the key.
// 	/// </summary>
// 	public string Key { get; }
//
// 	/// <summary>
// 	/// Initializes a new instance of the <see cref="DynamicDictionaryKey{TValue}"/> struct with the specified key.
// 	/// </summary>
// 	/// <param name="key">The key.</param>
// 	[DebuggerStepThrough]
// 	public DynamicDictionaryKey(string key) {
// 		if (string.IsNullOrEmpty(key))
// 			throw new ArgumentNullException(nameof(key));
//
// 		Key = key;
// 	}
//
// 	public override string ToString() => Key;
//
// 	public static implicit operator string(DynamicDictionaryKey<TValue> key) => key.Key;
// }
//
// /// <summary>
// /// A dictionary of dynamic properties.
// /// </summary>
// [DebuggerDisplay("{DebuggerToString(),nq}")]
// [DebuggerTypeProxy(typeof(DynamicDictionaryDebugView))]
// public class DynamicDictionary : IDictionary<string, object?>, IReadOnlyDictionary<string, object?> {
// 	/// <summary>
// 	/// Gets a read-only collection of dynamic properties.
// 	/// </summary>
// 	public static readonly DynamicDictionary Empty = new(new Dictionary<string, object?>(), true);
//
// 	readonly Dictionary<string, object?> _properties;
//
// 	/// <summary>
// 	/// Initializes a new instance of the <see cref="DynamicDictionary"/> class.
// 	/// </summary>
// 	public DynamicDictionary() : this(new Dictionary<string, object?>(), false) { }
//
// 	DynamicDictionary(Dictionary<string, object?> properties, bool isReadOnly) {
// 		_properties = properties;
// 		IsReadOnly   = isReadOnly;
// 	}
//
// 	public bool IsReadOnly { get; }
//
// 	object? IDictionary<string, object?>.this[string key] {
// 		get => _properties[key];
// 		set {
// 			CheckReadOnly();
// 			_properties[key] = value;
// 		}
// 	}
//
// 	ICollection<string> IDictionary<string, object?>.Keys =>
// 		_properties.Keys;
//
// 	ICollection<object?> IDictionary<string, object?>.Values =>
// 		_properties.Values;
//
// 	int ICollection<KeyValuePair<string, object?>>.Count =>
// 		_properties.Count;
//
// 	bool ICollection<KeyValuePair<string, object?>>.IsReadOnly =>
// 		IsReadOnly || ((ICollection<KeyValuePair<string, object?>>)_properties).IsReadOnly;
//
// 	void IDictionary<string, object?>.Add(string key, object? value) {
// 		CheckReadOnly();
// 		_properties.Add(key, value);
// 	}
//
// 	void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) {
// 		CheckReadOnly();
// 		((ICollection<KeyValuePair<string, object?>>)_properties).Add(item);
// 	}
//
// 	void ICollection<KeyValuePair<string, object?>>.Clear() {
// 		CheckReadOnly();
// 		_properties.Clear();
// 	}
//
// 	bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) =>
// 		_properties.Contains(item);
//
// 	bool IDictionary<string, object?>.ContainsKey(string key) =>
// 		_properties.ContainsKey(key);
//
// 	void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
// 		((ICollection<KeyValuePair<string, object?>>)_properties).CopyTo(array, arrayIndex);
//
// 	IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() =>
// 		_properties.GetEnumerator();
//
// 	IEnumerator IEnumerable.GetEnumerator() =>
// 		((IEnumerable)_properties).GetEnumerator();
//
// 	bool IDictionary<string, object?>.Remove(string key) {
// 		CheckReadOnly();
// 		return _properties.Remove(key);
// 	}
//
// 	bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) {
// 		CheckReadOnly();
// 		return ((ICollection<KeyValuePair<string, object?>>)_properties).Remove(item);
// 	}
//
// 	bool IDictionary<string, object?>.TryGetValue(string key, out object? value) => _properties.TryGetValue(key, out value);
//
// 	IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _properties.Keys;
//
// 	IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _properties.Values;
//
// 	int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => _properties.Count;
//
// 	object? IReadOnlyDictionary<string, object?>.this[string key] => _properties[key];
//
// 	bool IReadOnlyDictionary<string, object?>.ContainsKey(string key) => _properties.ContainsKey(key);
//
// 	bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value) => _properties.TryGetValue(key, out value);
//
// 	/// <summary>
// 	/// Sets the value associated with the specified key.
// 	/// </summary>
// 	/// <typeparam name="TValue">The value type.</typeparam>
// 	/// <param name="key">The key of the value to set.</param>
// 	/// <param name="value">The value.</param>
// 	public void Set<TValue>(DynamicDictionaryKey<TValue> key, TValue value) {
// 		CheckReadOnly();
// 		_properties[key.Key] = value;
// 	}
//
// 	/// <summary>
// 	/// Removes the value associated with the specified key.
// 	/// </summary>
// 	/// <typeparam name="TValue">The value type.</typeparam>
// 	/// <param name="key">The key of the value to set.</param>
// 	/// <returns>
// 	/// <c>true</c> if the element is successfully removed; otherwise, <c>false</c>.
// 	/// This method also returns <c>false</c> if key wasn't found.
// 	/// </returns>
// 	public bool Remove<TValue>(DynamicDictionaryKey<TValue> key) {
// 		CheckReadOnly();
// 		return _properties.Remove(key.Key);
// 	}
//
// 	void CheckReadOnly() {
// 		if (IsReadOnly) throw new NotSupportedException("Collection is read-only.");
// 	}
//
// 	string DebuggerToString() => $"Count = {_properties.Count}";
//
// 	sealed class DynamicDictionaryDebugView(DynamicDictionary collection) {
// 		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
// 		public KeyValuePair<string, object?>[] Items =>
// 			collection.Select(pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)).ToArray();
// 	}
//
// 	internal static bool DeepEquals(DynamicDictionary? x, DynamicDictionary? y) {
// 		var xValues = x?._properties;
// 		var yValues = y?._properties;
//
// 		if (ReferenceEquals(xValues, yValues)) return true;
//
// 		if (xValues == null || yValues == null) return false;
//
// 		if (xValues.Count != yValues.Count) return false;
//
// 		foreach (var kvp in xValues) {
// 			if (!yValues.TryGetValue(kvp.Key, out var value)) return false;
//
// 			if (!Equals(kvp.Value, value)) return false;
// 		}
//
// 		return true;
// 	}
//
//     /// <summary>
//     /// Tries to get the value associated with the specified key, with automatic type conversion where appropriate.
//     /// </summary>
//     /// <typeparam name="TValue">The value type.</typeparam>
//     /// <param name="key">The key of the <see cref="DynamicDictionaryKey{TValue}"/> to get.</param>
//     /// <param name="value">
//     /// When this method returns, contains the value associated with the specified key, if the key is found
//     /// and the value type matches the specified type. Otherwise, contains the default value for the type of
//     /// the <c>value</c> parameter.
//     /// </param>
//     /// <returns>
//     /// <c>true</c> if the <see cref="DynamicDictionary"/> contains an element with the specified key and value type; otherwise <c>false</c>.
//     /// </returns>
//     public bool TryGet<TValue>(DynamicDictionaryKey<TValue> key, out TValue? value) {
// 	    if (!_properties.TryGetValue(key, out var obj)) {
// 		    value = default;
// 		    return false;
// 	    }
//
// 	    // Direct type match
// 	    if (obj is TValue typedValue) {
// 		    value = typedValue;
// 		    return true;
// 	    }
//
// 	    // Handle null cases
// 	    if (obj is null) {
// 		    value = default;
// 		    return typeof(TValue).IsClass || Nullable.GetUnderlyingType(typeof(TValue)) != null; // Return true only for nullable types
// 	    }
//
// 	    var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
//
// 	    // Handle byte array and memory conversions
// 	    if (targetType == typeof(byte[]) || targetType == typeof(ReadOnlyMemory<byte>) || targetType == typeof(Memory<byte>)) {
// 		    try {
// 			    switch (obj) {
// 				    case byte[] byteArray: {
// 					    if (targetType == typeof(byte[]))
// 						    value = (TValue)(object)byteArray;
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (TValue)(object)new ReadOnlyMemory<byte>(byteArray);
// 					    else if (targetType == typeof(Memory<byte>))
// 						    value = (TValue)(object)new Memory<byte>(byteArray);
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
// 						    value = (TValue)(object)readOnlyMemory.ToArray();
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (TValue)(object)readOnlyMemory;
// 					    else if (targetType == typeof(Memory<byte>)) {
// 						    var memoryBytes = readOnlyMemory.ToArray();
// 						    value = (TValue)(object)new Memory<byte>(memoryBytes);
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
// 						    value = (TValue)(object)memory.ToArray();
// 					    else if (targetType == typeof(ReadOnlyMemory<byte>))
// 						    value = (TValue)(object)((ReadOnlyMemory<byte>)memory);
// 					    else if (targetType == typeof(Memory<byte>))
// 						    value = (TValue)(object)memory;
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
// 			    value = (TValue)enumValue;
// 			    return true;
// 		    }
//
// 		    value = default;
// 		    return false;
// 	    }
//
// 	    // Handle common value types with TryParse methods
// 	    if (targetType == typeof(bool) && bool.TryParse(stringValue, out var boolValue)) {
// 		    value = (TValue)(object)boolValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(byte) && byte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue)) {
// 		    value = (TValue)(object)byteValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(sbyte) && sbyte.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var sbyteValue)) {
// 		    value = (TValue)(object)sbyteValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(short) && short.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue)) {
// 		    value = (TValue)(object)shortValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(ushort) && ushort.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ushortValue)) {
// 		    value = (TValue)(object)ushortValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(int) && int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue)) {
// 		    value = (TValue)(object)intValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(uint) && uint.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue)) {
// 		    value = (TValue)(object)uintValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(long) && long.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue)) {
// 		    value = (TValue)(object)longValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(ulong) && ulong.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue)) {
// 		    value = (TValue)(object)ulongValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(float) && float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue)) {
// 		    value = (TValue)(object)floatValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(double) && double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue)) {
// 		    value = (TValue)(object)doubleValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(decimal) && decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue)) {
// 		    value = (TValue)(object)decimalValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(char) && stringValue.Length > 0) {
// 		    value = (TValue)(object)stringValue[0];
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(Guid) && Guid.TryParse(stringValue, out var guidValue)) {
// 		    value = (TValue)(object)guidValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanValue)) {
// 		    value = (TValue)(object)timeSpanValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(DateTime) && DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue)) {
// 		    value = (TValue)(object)dateTimeValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(DateTimeOffset) && DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetValue)) {
// 		    value = (TValue)(object)dateTimeOffsetValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(DateOnly) && DateOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnlyValue)) {
// 		    value = (TValue)(object)dateOnlyValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(TimeOnly) && TimeOnly.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timeOnlyValue)) {
// 		    value = (TValue)(object)timeOnlyValue;
// 		    return true;
// 	    }
//
// 	    if (targetType == typeof(Uri) && Uri.TryCreate(stringValue, UriKind.RelativeOrAbsolute, out var uriValue)) {
// 		    value = (TValue)(object)uriValue;
// 		    return true;
// 	    }
//
// 	    // If we couldn't convert, return default
// 	    value = default;
// 	    return false;
//     }
// }
