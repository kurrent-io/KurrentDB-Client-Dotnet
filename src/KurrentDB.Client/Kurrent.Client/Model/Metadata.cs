using System.Collections;
using System.Diagnostics;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents a collection of metadata as key-value pairs with additional helper methods.
/// Note: This class is not thread-safe. Synchronization must be provided by the caller when accessed from multiple threads.
/// </summary>
[PublicAPI]
[DebuggerDisplay("{ToDebugString(prettyPrint: true)}")]
public class Metadata : IEnumerable<KeyValuePair<string, object?>> // IDictionary<string, object?>
{
	readonly Dictionary<string, object?> _dictionary;

	/// <summary>
	/// Initializes a new, empty instance of the Metadata class using ordinal string comparison.
	/// </summary>
	public Metadata() =>
		_dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the Metadata class from an existing metadata instance.
	/// </summary>
	/// <param name="metadata">The metadata to copy from.</param>
	public Metadata(Metadata metadata) =>
		_dictionary = new Dictionary<string, object?>(metadata._dictionary, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the Metadata class from a dictionary.
	/// </summary>
	/// <param name="items">The dictionary to copy from.</param>
	public Metadata(Dictionary<string, object?> items) =>
		_dictionary = new Dictionary<string, object?>(items, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the Metadata class from a collection of key-value pairs.
	/// </summary>
	/// <param name="items">The collection of key-value pairs to copy from.</param>
	public Metadata(IEnumerable<KeyValuePair<string, object?>> items) {
		// Skip the dictionary constructor to avoid ambiguity
		_dictionary = items.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
	}

	#region . IDictionary .

	/// <summary>
	/// Gets the number of elements in the collection.
	/// </summary>
	public int Count => _dictionary.Count;

	/// <summary>
	/// Gets a value indicating whether the dictionary is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Gets an collection that contains the keys in the dictionary.
	/// </summary>
	public ICollection<string> Keys => _dictionary.Keys;

	/// <summary>
	/// Gets an collection that contains the values in the dictionary.
	/// </summary>
	public ICollection<object?> Values => _dictionary.Values;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to look up or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">The key does not exist in the collection (when getting).</exception>
	public object? this[string key] {
		get => _dictionary[key];
		set => _dictionary[key] = value;
	}

	/// <summary>
	/// Adds the specified key and value to the dictionary.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	public void Add(string key, object? value) => _dictionary.Add(key, value);

	/// <summary>
	/// Adds the specified key-value pair to the dictionary.
	/// </summary>
	/// <param name="item">The key-value pair to add.</param>
	public void Add(KeyValuePair<string, object?> item) => _dictionary.Add(item.Key, item.Value);

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	public void Clear() => _dictionary.Clear();

	/// <summary>
	/// Determines whether the dictionary contains the specified key-value pair.
	/// </summary>
	/// <param name="item">The key-value pair to locate.</param>
	/// <returns>true if the dictionary contains the specified key-value pair; otherwise, false.</returns>
	public bool Contains(KeyValuePair<string, object?> item) =>
		_dictionary.TryGetValue(item.Key, out var value) &&
		Equals(value, item.Value);

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
	public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

	/// <summary>
	/// Copies the elements of the dictionary to an array, starting at the specified array index.
	/// </summary>
	/// <param name="array">The destination array.</param>
	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
		((ICollection<KeyValuePair<string, object?>>)_dictionary).CopyTo(array, arrayIndex);

	/// <summary>
	/// Removes the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
	public bool Remove(string key) => _dictionary.Remove(key);

	/// <summary>
	/// Removes the specified key-value pair from the dictionary.
	/// </summary>
	/// <param name="item">The key-value pair to remove.</param>
	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
	public bool Remove(KeyValuePair<string, object?> item) =>
		Contains(item) && _dictionary.Remove(item.Key);

	// /// <summary>
	// /// Tries to get the value associated with the specified key.
	// /// </summary>
	// /// <param name="key">The key to look up.</param>
	// /// <param name="value">When this method returns, contains the value associated with the specified key if found; otherwise, the default value.</param>
	// /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
	// public bool TryGetValue(string key, out object? value) => _dictionary.TryGetValue(key, out value);

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dictionary.GetEnumerator();

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	#region . Value Retrieval Methods .

	/// <summary>
	/// Tries to get a typed value from the metadata, with automatic conversion from strings.
	/// </summary>
	/// <typeparam name="T">The type to get or convert to.</typeparam>
	/// <param name="key">The key to retrieve.</param>
	/// <param name="value">When this method returns, contains the value if found and successfully converted; otherwise, default(T).</param>
	/// <returns>true if the key was found and the value could be converted to type T; otherwise, false.</returns>
	public bool TryGet<T>(string key, out T? value) {
		// Check if key exists
		if (!_dictionary.TryGetValue(key, out var obj)) {
			value = default;
			return false;
		}

		switch (obj) {
			// Direct type match (most common case)
			case T typedValue:
				value = typedValue;
				return true;

			// Handle null case
			case null:
				value = default;
				return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null;

			// If the object is a string, try to convert it to type T
			case string stringValue: {
				var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

				// Handle basic types with TypeConverter
				var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
				if (converter.CanConvertFrom(typeof(string)))
					try {
						value = (T)converter.ConvertFromInvariantString(stringValue)!;
						return true;
					}
					catch {
						value = default;
						return false;
					}

                // Handle enum types
                if (targetType.IsEnum) {
                    try {
                        if(Enum.TryParse(targetType, stringValue, ignoreCase: true, out var enumValue)) {
                            value = (T)enumValue;
                            return true;
                        }

                        value = default;
                        return false;
                    }
                    catch {
                        value = default;
                        return false;
                    }
                }

				break;
			}
		}

		// If we got here, we couldn't convert
		value = default;
		return false;
	}

    /// <summary>
    /// Gets the value associated with the specified key as the specified type, or a default value if the key is not found.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="key">The key to locate.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or if the value cannot be cast.</param>
    /// <returns>The value associated with the specified key cast to type T, or the default value if the key is not found or the value cannot be cast.</returns>
    public T? GetOrDefault<T>(string key, T? defaultValue = default) =>
        TryGet<T>(key, out var typedValue)
            ? typedValue
            : defaultValue;

    // public T? Get<T>(string key, T? defaultValue = default) =>
    //     TryGet<T>(key, out var typedValue)
    //         ? typedValue
    //         : defaultValue;

    // public T GetOrDefault<T>(string key, T defaultValue) where T : notnull =>
    //     TryGet<T>(key, out var typedValue) && typedValue is not null
    //         ? typedValue
    //         : defaultValue;

    public T Get<T>(string key) =>
        TryGet<T>(key, out var typedValue) && typedValue is not null
            ? typedValue
            : throw new KeyNotFoundException($"Key '{key}' not found in metadata or value is null.");

	// /// <summary>
	// /// Gets the value associated with the specified key as the specified type, or a default value if the key is not found.
	// /// </summary>
	// /// <typeparam name="T">The type to cast the value to.</typeparam>
	// /// <param name="key">The key to locate.</param>
	// /// <param name="defaultValue">The default value to return if the key is not found or if the value cannot be cast.</param>
	// /// <returns>The value associated with the specified key cast to type T, or the default value if the key is not found or the value cannot be cast.</returns>
	// public T Get<T>(string key, T defaultValue) =>
 //        ContainsKey(key) && TryGet<T>(key, out var typedValue) ? typedValue : defaultValue;

	// /// <summary>
	// /// Gets the value associated with the specified key as the specified type.
	// /// </summary>
	// /// <typeparam name="T">The type to cast the value to.</typeparam>
	// /// <param name="key">The key to locate.</param>
	// /// <returns>The value associated with the specified key cast to type T.</returns>
	// /// <exception cref="KeyNotFoundException">Thrown if the key is not present in the metadata.</exception>
	// /// <exception cref="InvalidCastException">Thrown if the value cannot be cast to the specified type.</exception>
	// public T Get<T>(string key) {
	// 	if (!ContainsKey(key))
	// 		throw new KeyNotFoundException($"Key '{key}' not found in metadata.");
	//
	// 	if (TryGet<T>(key, out var typedValue))
	// 		return typedValue;
	//
	// 	throw new InvalidCastException(
	// 		$"Cannot cast value of type '{typedValue?.GetType().Name ?? "null"}' to '{typeof(T).Name}' for key '{key}'.");
	// }

	#endregion

	#region . Modification Methods .

	/// <summary>
	/// Adds or updates a key-value pair in the metadata.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="key">The key to add or update.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata With<T>(string key, T? value) {
		_dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Sets a single key-value pair in the metadata.
	/// </summary>
	/// <param name="key">The key to set.</param>
	/// <param name="value">The value to associate with the key.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata With(string key, object? value) {
		_dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with entries from another metadata object.
	/// </summary>
	/// <param name="items">The metadata containing key-value pairs to be added or updated.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithMany(Metadata items) {
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with additional key-value pairs.
	/// </summary>
	/// <param name="items">An array of key-value pairs to update the metadata with.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithMany(params KeyValuePair<string, object?>[] items) {
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with additional key-value pairs.
	/// </summary>
	/// <param name="items">A collection of key-value pairs to update the metadata with.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithMany(IEnumerable<KeyValuePair<string, object?>> items) {
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata Without(string key) {
		_dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata.
	/// </summary>
	/// <param name="keys">The collection of keys to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutMany(IEnumerable<string> keys) {
		foreach (var key in keys) _dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata.
	/// </summary>
	/// <param name="keys">The collection of keys to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutMany(params string[] keys) {
		foreach (var key in keys) _dictionary.Remove(key);
		return this;
	}

	#endregion

	#region . Conditional Modification Methods .

	/// <summary>
	/// Adds or updates a key-value pair in the metadata only if a condition is true.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="key">The key to add or update if the condition is true.</param>
	/// <param name="value">The value to set if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithIf<T>(bool condition, string key, T? value) {
		if (condition) _dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Adds or updates a key-value pair in the metadata only if a condition is true.
	/// The value is generated using a factory function that is only called if the condition is true.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="key">The key to add or update if the condition is true.</param>
	/// <param name="valueFactory">A function that produces the value to set if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithIf<T>(bool condition, string key, Func<T?> valueFactory) {
		if (condition) _dictionary[key] = valueFactory();
		return this;
	}

	/// <summary>
	/// Adds or updates a key-value pair in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="key">The key to add or update if the predicate returns true.</param>
	/// <param name="value">The value to set if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, T? value) {
		if (predicate(this)) _dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Adds or updates a key-value pair in the metadata only if the predicate evaluates to true.
	/// The value is generated using a factory function that is only called if the predicate returns true.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="key">The key to add or update if the predicate returns true.</param>
	/// <param name="valueFactory">A function that produces the value to set if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, Func<T?> valueFactory) {
		if (predicate(this)) _dictionary[key] = valueFactory();
		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The metadata containing key-value pairs to be added or updated if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, Metadata items) {
		if (condition)
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, IDictionary<string, object?> items) {
		if (condition)
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The key-value pairs to add or update if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, params KeyValuePair<string, object?>[] items) {
		if (condition)
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
	/// The pairs are generated using a factory function that is only called if the condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="itemsFactory">A function that produces the key-value pairs to add or update if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, Func<IEnumerable<KeyValuePair<string, object?>>> itemsFactory) {
		if (condition)
			foreach (var item in itemsFactory())
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The metadata containing key-value pairs to be added or updated if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, Metadata items) {
		if (predicate(this))
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, IDictionary<string, object?> items) {
		if (predicate(this))
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The key-value pairs to add or update if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, params KeyValuePair<string, object?>[] items) {
		if (predicate(this))
			foreach (var item in items)
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
	/// The pairs are generated using a factory function that is only called if the predicate returns true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="itemsFactory">A function that produces the key-value pairs to add or update if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, Func<IEnumerable<KeyValuePair<string, object?>>> itemsFactory) {
		if (predicate(this))
			foreach (var item in itemsFactory())
				_dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="key">The key to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutIf(bool condition, string key) {
		if (condition) _dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="keys">The collection of keys to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(bool condition, IEnumerable<string> keys) {
		if (condition)
			foreach (var key in keys)
				_dictionary.Remove(key);

		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="keys">The keys to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(bool condition, params string[] keys) {
		if (condition)
			foreach (var key in keys)
				_dictionary.Remove(key);

		return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="key">The key to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutIf(Func<Metadata, bool> predicate, string key) {
		if (predicate(this)) _dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="keys">The collection of keys to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, IEnumerable<string> keys) {
		if (predicate(this))
			foreach (var key in keys)
				_dictionary.Remove(key);

		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="keys">The keys to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, params string[] keys) {
		if (predicate(this))
			foreach (var key in keys)
				_dictionary.Remove(key);

		return this;
	}

    /// <summary>
    /// Applies a transformation function to the metadata.
    /// </summary>
    /// <param name="transform">The transformation function to apply if the condition is true.</param>
    /// <returns>This metadata instance for method chaining.</returns>
    public Metadata Transform(Action<Metadata> transform) {
        transform(this);
        return this;
    }

    /// <summary>
	/// Applies a transformation function to the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="transform">The transformation function to apply if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata TransformIf(bool condition, Func<Metadata, Metadata> transform) =>
		condition ? transform(this) : this;

	/// <summary>
	/// Applies a transformation function to the metadata only if a predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="transform">The transformation function to apply if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata TransformIf(Func<Metadata, bool> predicate, Func<Metadata, Metadata> transform) =>
		predicate(this) ? transform(this) : this;

	#endregion

	/// <summary>
	/// Returns a string representation of the metadata suitable for debugging.
	/// </summary>
	public override string ToString() =>
		$"Metadata({Count} items): {{{string.Join(", ", _dictionary.Select(p => $"{p.Key}={p.Value}"))}}}";

	/// <summary>
	/// Creates a debug view.
	/// </summary>
	public string ToDebugString(bool prettyPrint = false) =>
		!prettyPrint ? ToString() : $"Metadata:\n{string.Join("\n", _dictionary.Select(p => $"  {p.Key} = {p.Value ?? "null"}"))}";

	/// <summary>
	/// Defines a custom operator for a class or struct to implement specific behavior for operations involving its instances.
	/// </summary>
	public static implicit operator Metadata(Dictionary<string, object?> dictionary) => new(dictionary);
}




// /// <summary>
// /// Represents a collection of metadata as key-value pairs with additional helper methods.
// /// Note: This class is not thread-safe. Synchronization must be provided by the caller when accessed from multiple threads.
// /// </summary>
// [PublicAPI]
// [DebuggerDisplay("{ToDebugString(prettyPrint: true)}")]
// public class Metadata : IDictionary<string, object?> {
// 	readonly Dictionary<string, object?> _dictionary;
//
// 	/// <summary>
// 	/// Initializes a new, empty instance of the Metadata class using ordinal string comparison.
// 	/// </summary>
// 	public Metadata() =>
// 		_dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
//
// 	/// <summary>
// 	/// Initializes a new instance of the Metadata class from an existing metadata instance.
// 	/// </summary>
// 	/// <param name="metadata">The metadata to copy from.</param>
// 	public Metadata(Metadata metadata) =>
// 		_dictionary = new Dictionary<string, object?>(metadata._dictionary, StringComparer.OrdinalIgnoreCase);
//
// 	/// <summary>
// 	/// Initializes a new instance of the Metadata class from a dictionary.
// 	/// </summary>
// 	/// <param name="items">The dictionary to copy from.</param>
// 	public Metadata(Dictionary<string, object?> items) =>
// 		_dictionary = new Dictionary<string, object?>(items, StringComparer.OrdinalIgnoreCase);
//
// 	/// <summary>
// 	/// Initializes a new instance of the Metadata class from a collection of key-value pairs.
// 	/// </summary>
// 	/// <param name="items">The collection of key-value pairs to copy from.</param>
// 	public Metadata(IEnumerable<KeyValuePair<string, object?>> items) {
// 		// Skip the dictionary constructor to avoid ambiguity
// 		_dictionary = items.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
// 	}
//
// 	#region . IDictionary .
//
// 	/// <summary>
// 	/// Gets the number of elements in the collection.
// 	/// </summary>
// 	public int Count => _dictionary.Count;
//
// 	/// <summary>
// 	/// Gets a value indicating whether the dictionary is read-only.
// 	/// </summary>
// 	public bool IsReadOnly => false;
//
// 	/// <summary>
// 	/// Gets an collection that contains the keys in the dictionary.
// 	/// </summary>
// 	public ICollection<string> Keys => _dictionary.Keys;
//
// 	/// <summary>
// 	/// Gets an collection that contains the values in the dictionary.
// 	/// </summary>
// 	public ICollection<object?> Values => _dictionary.Values;
//
// 	/// <summary>
// 	/// Gets or sets the value associated with the specified key.
// 	/// </summary>
// 	/// <param name="key">The key to look up or set.</param>
// 	/// <returns>The value associated with the specified key.</returns>
// 	/// <exception cref="KeyNotFoundException">The key does not exist in the collection (when getting).</exception>
// 	public object? this[string key] {
// 		get => _dictionary[key];
// 		set => _dictionary[key] = value;
// 	}
//
// 	/// <summary>
// 	/// Adds the specified key and value to the dictionary.
// 	/// </summary>
// 	/// <param name="key">The key of the element to add.</param>
// 	/// <param name="value">The value of the element to add.</param>
// 	public void Add(string key, object? value) => _dictionary.Add(key, value);
//
// 	/// <summary>
// 	/// Adds the specified key-value pair to the dictionary.
// 	/// </summary>
// 	/// <param name="item">The key-value pair to add.</param>
// 	public void Add(KeyValuePair<string, object?> item) => _dictionary.Add(item.Key, item.Value);
//
// 	/// <summary>
// 	/// Removes all keys and values from the dictionary.
// 	/// </summary>
// 	public void Clear() => _dictionary.Clear();
//
// 	/// <summary>
// 	/// Determines whether the dictionary contains the specified key-value pair.
// 	/// </summary>
// 	/// <param name="item">The key-value pair to locate.</param>
// 	/// <returns>true if the dictionary contains the specified key-value pair; otherwise, false.</returns>
// 	public bool Contains(KeyValuePair<string, object?> item) =>
// 		_dictionary.TryGetValue(item.Key, out var value) &&
// 		Equals(value, item.Value);
//
// 	/// <summary>
// 	/// Determines whether the dictionary contains the specified key.
// 	/// </summary>
// 	/// <param name="key">The key to locate.</param>
// 	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
// 	public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
//
// 	/// <summary>
// 	/// Copies the elements of the dictionary to an array, starting at the specified array index.
// 	/// </summary>
// 	/// <param name="array">The destination array.</param>
// 	/// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
// 	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
// 		((ICollection<KeyValuePair<string, object?>>)_dictionary).CopyTo(array, arrayIndex);
//
// 	/// <summary>
// 	/// Removes the specified key from the dictionary.
// 	/// </summary>
// 	/// <param name="key">The key to remove.</param>
// 	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
// 	public bool Remove(string key) => _dictionary.Remove(key);
//
// 	/// <summary>
// 	/// Removes the specified key-value pair from the dictionary.
// 	/// </summary>
// 	/// <param name="item">The key-value pair to remove.</param>
// 	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
// 	public bool Remove(KeyValuePair<string, object?> item) =>
// 		Contains(item) && _dictionary.Remove(item.Key);
//
// 	/// <summary>
// 	/// Tries to get the value associated with the specified key.
// 	/// </summary>
// 	/// <param name="key">The key to look up.</param>
// 	/// <param name="value">When this method returns, contains the value associated with the specified key if found; otherwise, the default value.</param>
// 	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
// 	public bool TryGetValue(string key, out object? value) => _dictionary.TryGetValue(key, out value);
//
// 	/// <summary>
// 	/// Returns an enumerator that iterates through the collection.
// 	/// </summary>
// 	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
// 	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _dictionary.GetEnumerator();
//
// 	/// <summary>
// 	/// Returns an enumerator that iterates through the collection.
// 	/// </summary>
// 	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
// 	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//
// 	#endregion
//
// 	#region . Value Retrieval Methods .
//
// 	/// <summary>
// 	/// Tries to get a typed value from the metadata, with automatic conversion from strings.
// 	/// </summary>
// 	/// <typeparam name="T">The type to get or convert to.</typeparam>
// 	/// <param name="key">The key to retrieve.</param>
// 	/// <param name="value">When this method returns, contains the value if found and successfully converted; otherwise, default(T).</param>
// 	/// <returns>true if the key was found and the value could be converted to type T; otherwise, false.</returns>
// 	public bool TryGet<T>(string key, out T? value) {
// 		// Check if key exists
// 		if (!TryGetValue(key, out var obj)) {
// 			value = default;
// 			return false;
// 		}
//
// 		switch (obj) {
// 			// Direct type match (most common case)
// 			case T typedValue:
// 				value = typedValue;
// 				return true;
//
// 			// Handle null case
// 			case null:
// 				value = default;
// 				return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) != null;
//
// 			// If the object is a string, try to convert it to type T
// 			case string stringValue: {
// 				var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
//
// 				// Handle basic types with TypeConverter
// 				var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
// 				if (converter.CanConvertFrom(typeof(string)))
// 					try {
// 						value = (T)converter.ConvertFromInvariantString(stringValue)!;
// 						return true;
// 					}
// 					catch {
// 						value = default;
// 						return false;
// 					}
//
//                 // Handle enum types
//                 if (targetType.IsEnum) {
//                     try {
//                         if(Enum.TryParse(targetType, stringValue, ignoreCase: true, out var enumValue)) {
//                             value = (T)enumValue;
//                             return true;
//                         }
//
//                         value = default;
//                         return false;
//                     }
//                     catch {
//                         value = default;
//                         return false;
//                     }
//                 }
//
// 				break;
// 			}
// 		}
//
// 		// If we got here, we couldn't convert
// 		value = default;
// 		return false;
// 	}
//
// 	/// <summary>
// 	/// Gets the value associated with the specified key as the specified type, or a default value if the key is not found.
// 	/// </summary>
// 	/// <typeparam name="T">The type to cast the value to.</typeparam>
// 	/// <param name="key">The key to locate.</param>
// 	/// <param name="defaultValue">The default value to return if the key is not found or if the value cannot be cast.</param>
// 	/// <returns>The value associated with the specified key cast to type T, or the default value if the key is not found or the value cannot be cast.</returns>
// 	public T? GetOrDefault<T>(string key, T? defaultValue = default) =>
// 		TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;
//
// 	/// <summary>
// 	/// Gets the value associated with the specified key as the specified type, or a default value if the key is not found.
// 	/// </summary>
// 	/// <typeparam name="T">The type to cast the value to.</typeparam>
// 	/// <param name="key">The key to locate.</param>
// 	/// <param name="defaultValue">The default value to return if the key is not found or if the value cannot be cast.</param>
// 	/// <returns>The value associated with the specified key cast to type T, or the default value if the key is not found or the value cannot be cast.</returns>
// 	public T Get<T>(string key, T defaultValue) =>
// 		TryGetValue(key, out var value) && value is T typedObject ? typedObject : defaultValue;
//
// 	/// <summary>
// 	/// Gets the value associated with the specified key as the specified type.
// 	/// </summary>
// 	/// <typeparam name="T">The type to cast the value to.</typeparam>
// 	/// <param name="key">The key to locate.</param>
// 	/// <returns>The value associated with the specified key cast to type T.</returns>
// 	/// <exception cref="KeyNotFoundException">Thrown if the key is not present in the metadata.</exception>
// 	/// <exception cref="InvalidCastException">Thrown if the value cannot be cast to the specified type.</exception>
// 	public T Get<T>(string key) {
// 		if (!TryGetValue(key, out var value))
// 			throw new KeyNotFoundException($"Key '{key}' not found in metadata.");
//
// 		if (value is T typedObject)
// 			return typedObject;
//
// 		throw new InvalidCastException(
// 			$"Cannot cast value of type '{value?.GetType().Name ?? "null"}' to '{typeof(T).Name}' for key '{key}'.");
// 	}
//
// 	#endregion
//
// 	#region . Modification Methods .
//
// 	/// <summary>
// 	/// Adds or updates a key-value pair in the metadata.
// 	/// </summary>
// 	/// <typeparam name="T">The type of the value.</typeparam>
// 	/// <param name="key">The key to add or update.</param>
// 	/// <param name="value">The value to set.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata With<T>(string key, T? value) {
// 		_dictionary[key] = value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets a single key-value pair in the metadata.
// 	/// </summary>
// 	/// <param name="key">The key to set.</param>
// 	/// <param name="value">The value to associate with the key.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata With(string key, object? value) {
// 		_dictionary[key] = value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Updates the current metadata with entries from another metadata object.
// 	/// </summary>
// 	/// <param name="items">The metadata containing key-value pairs to be added or updated.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithMany(Metadata items) {
// 		foreach (var item in items) _dictionary[item.Key] = item.Value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Updates the current metadata with additional key-value pairs.
// 	/// </summary>
// 	/// <param name="items">An array of key-value pairs to update the metadata with.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithMany(params KeyValuePair<string, object?>[] items) {
// 		foreach (var item in items) _dictionary[item.Key] = item.Value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Updates the current metadata with additional key-value pairs.
// 	/// </summary>
// 	/// <param name="items">A collection of key-value pairs to update the metadata with.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithMany(IEnumerable<KeyValuePair<string, object?>> items) {
// 		foreach (var item in items) _dictionary[item.Key] = item.Value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes the specified key from the metadata.
// 	/// </summary>
// 	/// <param name="key">The key to remove.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata Without(string key) {
// 		_dictionary.Remove(key);
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata.
// 	/// </summary>
// 	/// <param name="keys">The collection of keys to remove.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutMany(IEnumerable<string> keys) {
// 		foreach (var key in keys) _dictionary.Remove(key);
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata.
// 	/// </summary>
// 	/// <param name="keys">The collection of keys to remove.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutMany(params string[] keys) {
// 		foreach (var key in keys) _dictionary.Remove(key);
// 		return this;
// 	}
//
// 	#endregion
//
// 	#region . Conditional Modification Methods .
//
// 	/// <summary>
// 	/// Adds or updates a key-value pair in the metadata only if a condition is true.
// 	/// </summary>
// 	/// <typeparam name="T">The type of the value.</typeparam>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="key">The key to add or update if the condition is true.</param>
// 	/// <param name="value">The value to set if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithIf<T>(bool condition, string key, T? value) {
// 		if (condition) _dictionary[key] = value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates a key-value pair in the metadata only if a condition is true.
// 	/// The value is generated using a factory function that is only called if the condition is true.
// 	/// </summary>
// 	/// <typeparam name="T">The type of the value.</typeparam>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="key">The key to add or update if the condition is true.</param>
// 	/// <param name="valueFactory">A function that produces the value to set if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithIf<T>(bool condition, string key, Func<T?> valueFactory) {
// 		if (condition) _dictionary[key] = valueFactory();
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates a key-value pair in the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <typeparam name="T">The type of the value.</typeparam>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="key">The key to add or update if the predicate returns true.</param>
// 	/// <param name="value">The value to set if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, T? value) {
// 		if (predicate(this)) _dictionary[key] = value;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates a key-value pair in the metadata only if the predicate evaluates to true.
// 	/// The value is generated using a factory function that is only called if the predicate returns true.
// 	/// </summary>
// 	/// <typeparam name="T">The type of the value.</typeparam>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="key">The key to add or update if the predicate returns true.</param>
// 	/// <param name="valueFactory">A function that produces the value to set if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, Func<T?> valueFactory) {
// 		if (predicate(this)) _dictionary[key] = valueFactory();
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="items">The metadata containing key-value pairs to be added or updated if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(bool condition, Metadata items) {
// 		if (condition)
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(bool condition, IDictionary<string, object?> items) {
// 		if (condition)
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="items">The key-value pairs to add or update if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(bool condition, params KeyValuePair<string, object?>[] items) {
// 		if (condition)
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
// 	/// The pairs are generated using a factory function that is only called if the condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="itemsFactory">A function that produces the key-value pairs to add or update if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(bool condition, Func<IEnumerable<KeyValuePair<string, object?>>> itemsFactory) {
// 		if (condition)
// 			foreach (var item in itemsFactory())
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="items">The metadata containing key-value pairs to be added or updated if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(Func<Metadata, bool> predicate, Metadata items) {
// 		if (predicate(this))
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(Func<Metadata, bool> predicate, IDictionary<string, object?> items) {
// 		if (predicate(this))
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="items">The key-value pairs to add or update if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(Func<Metadata, bool> predicate, params KeyValuePair<string, object?>[] items) {
// 		if (predicate(this))
// 			foreach (var item in items)
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
// 	/// The pairs are generated using a factory function that is only called if the predicate returns true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="itemsFactory">A function that produces the key-value pairs to add or update if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithManyIf(Func<Metadata, bool> predicate, Func<IEnumerable<KeyValuePair<string, object?>>> itemsFactory) {
// 		if (predicate(this))
// 			foreach (var item in itemsFactory())
// 				_dictionary[item.Key] = item.Value;
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes the specified key from the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="key">The key to remove if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutIf(bool condition, string key) {
// 		if (condition) _dictionary.Remove(key);
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="keys">The collection of keys to remove if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutManyIf(bool condition, IEnumerable<string> keys) {
// 		if (condition)
// 			foreach (var key in keys)
// 				_dictionary.Remove(key);
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="keys">The keys to remove if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutManyIf(bool condition, params string[] keys) {
// 		if (condition)
// 			foreach (var key in keys)
// 				_dictionary.Remove(key);
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes the specified key from the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="key">The key to remove if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutIf(Func<Metadata, bool> predicate, string key) {
// 		if (predicate(this)) _dictionary.Remove(key);
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="keys">The collection of keys to remove if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, IEnumerable<string> keys) {
// 		if (predicate(this))
// 			foreach (var key in keys)
// 				_dictionary.Remove(key);
//
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="keys">The keys to remove if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, params string[] keys) {
// 		if (predicate(this))
// 			foreach (var key in keys)
// 				_dictionary.Remove(key);
//
// 		return this;
// 	}
//
//     /// <summary>
//     /// Applies a transformation function to the metadata.
//     /// </summary>
//     /// <param name="transform">The transformation function to apply if the condition is true.</param>
//     /// <returns>This metadata instance for method chaining.</returns>
//     public Metadata Transform(Action<Metadata> transform) {
//         transform(this);
//         return this;
//     }
//
//     /// <summary>
// 	/// Applies a transformation function to the metadata only if a condition is true.
// 	/// </summary>
// 	/// <param name="condition">The condition to evaluate.</param>
// 	/// <param name="transform">The transformation function to apply if the condition is true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata TransformIf(bool condition, Func<Metadata, Metadata> transform) =>
// 		condition ? transform(this) : this;
//
// 	/// <summary>
// 	/// Applies a transformation function to the metadata only if a predicate evaluates to true.
// 	/// </summary>
// 	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
// 	/// <param name="transform">The transformation function to apply if the predicate returns true.</param>
// 	/// <returns>This metadata instance for method chaining.</returns>
// 	public Metadata TransformIf(Func<Metadata, bool> predicate, Func<Metadata, Metadata> transform) =>
// 		predicate(this) ? transform(this) : this;
//
// 	#endregion
//
// 	/// <summary>
// 	/// Returns a string representation of the metadata suitable for debugging.
// 	/// </summary>
// 	public override string ToString() =>
// 		$"Metadata({Count} items): {{{string.Join(", ", _dictionary.Select(p => $"{p.Key}={p.Value}"))}}}";
//
// 	/// <summary>
// 	/// Creates a debug view.
// 	/// </summary>
// 	public string ToDebugString(bool prettyPrint = false) =>
// 		!prettyPrint ? ToString() : $"Metadata:\n{string.Join("\n", _dictionary.Select(p => $"  {p.Key} = {p.Value ?? "null"}"))}";
//
// 	/// <summary>
// 	/// Defines a custom operator for a class or struct to implement specific behavior for operations involving its instances.
// 	/// </summary>
// 	public static implicit operator Metadata(Dictionary<string, object?> dictionary) => new(dictionary);
// }
