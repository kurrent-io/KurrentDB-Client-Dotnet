using System.Collections;
using System.Diagnostics;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents a collection of metadata as key-value pairs with additional helper methods.
/// Note: This class is not thread-safe. Synchronization must be provided by the caller when accessed from multiple threads.
/// </summary>
[PublicAPI]
[DebuggerDisplay("{ToDebugString(prettyPrint: true)}")]
public class Metadata : IEnumerable<KeyValuePair<string, object?>> {
	readonly Dictionary<string, object?> _dictionary;
	bool _isLocked;

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

    public static Metadata Create(Dictionary<string, object?> items) => new(items);
    public static Metadata Create(Dictionary<string, string?> items) => new(items.ToDictionary(x => x.Key, static object? (kvp) => kvp.Value));

	/// <summary>
	/// Locks this metadata instance, making it immutable. Direct mutations will throw an exception.
	/// Use CreateUnlockedCopy() method to create a mutable copy when modifications are needed.
	/// </summary>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <example>
	/// <code>
	/// var metadata = new Metadata().With("key", "value").Lock();
	///
	/// // This will throw an InvalidOperationException
	/// metadata["key"] = "new value";
	///
	/// // Create an unlocked copy for modifications
	/// var unlocked = metadata.CreateUnlockedCopy();
	/// var modified = unlocked.With("key", "new value");
	/// </code>
	/// </example>
	public Metadata Lock() {
		_isLocked = true;
		return this;
	}

	/// <summary>
	/// Creates an unlocked copy of this metadata instance that can be modified.
	/// The returned copy will have IsLocked = false regardless of the current instance's lock state.
	/// </summary>
	/// <returns>A new unlocked metadata instance containing the same key-value pairs.</returns>
	/// <example>
	/// <code>
	/// var locked = metadata.Lock();
	/// var unlocked = locked.CreateUnlockedCopy();
	///
	/// unlocked.IsLocked;  // false
	/// unlocked.With("new", "value");  // works normally
	///
	/// // Fluent API chains
	/// var result = locked.CreateUnlockedCopy()
	///     .With("key", "value")
	///     .Lock();
	/// </code>
	/// </example>
	public Metadata CreateUnlockedCopy() => new(this);

    /// <summary>
	/// Throws an InvalidOperationException if this metadata instance is locked.
	/// </summary>
	Metadata ThrowIfLocked() =>
        _isLocked ? throw new InvalidOperationException("Cannot modify locked metadata. Use CreateUnlockedCopy() to create a mutable copy.") : this;

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
	/// Gets a value indicating whether this metadata instance is locked and cannot be modified directly.
	/// </summary>
	public bool IsLocked => _isLocked;

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
	/// <exception cref="InvalidOperationException">Thrown when attempting to set a value on a locked metadata instance.</exception>
	public object? this[string key] {
		get => _dictionary[key];
		set {
			ThrowIfLocked();
			_dictionary[key] = value;
		}
	}

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when attempting to clear a locked metadata instance.</exception>
	public void Clear() {
		ThrowIfLocked();
		_dictionary.Clear();
	}

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
	public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

	/// <summary>
	/// Removes the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to remove from a locked metadata instance.</exception>
	public bool Remove(string key) {
		ThrowIfLocked();
		return _dictionary.Remove(key);
	}

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

    public T Get<T>(string key) =>
        TryGet<T>(key, out var typedValue) && typedValue is not null
            ? typedValue
            : throw new KeyNotFoundException($"Key '{key}' not found in metadata or value is null.");

	#endregion

	#region . Modification Methods .

	/// <summary>
	/// Adds or updates a key-value pair in the metadata.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	/// <param name="key">The key to add or update.</param>
	/// <param name="value">The value to set.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata With<T>(string key, T? value) {
		ThrowIfLocked();
		_dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Sets a single key-value pair in the metadata.
	/// </summary>
	/// <param name="key">The key to set.</param>
	/// <param name="value">The value to associate with the key.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata With(string key, object? value) {
		ThrowIfLocked();
		_dictionary[key] = value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with entries from another metadata object.
	/// </summary>
	/// <param name="items">The metadata containing key-value pairs to be added or updated.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata WithMany(Metadata items) {
		ThrowIfLocked();
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with additional key-value pairs.
	/// </summary>
	/// <param name="items">An array of key-value pairs to update the metadata with.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata WithMany(params KeyValuePair<string, object?>[] items) {
		ThrowIfLocked();
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Updates the current metadata with additional key-value pairs.
	/// </summary>
	/// <param name="items">A collection of key-value pairs to update the metadata with.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata WithMany(IEnumerable<KeyValuePair<string, object?>> items) {
		ThrowIfLocked();
		foreach (var item in items) _dictionary[item.Key] = item.Value;
		return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata Without(string key) {
		ThrowIfLocked();
		_dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata.
	/// </summary>
	/// <param name="keys">The collection of keys to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata WithoutMany(IEnumerable<string> keys) {
		ThrowIfLocked();
		foreach (var key in keys) _dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata.
	/// </summary>
	/// <param name="keys">The collection of keys to remove.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
	public Metadata WithoutMany(params string[] keys) {
		ThrowIfLocked();
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
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance and condition is true.</exception>
	public Metadata WithIf<T>(bool condition, string key, T? value) {
		if (!condition) return this;
		ThrowIfLocked();
		_dictionary[key] = value;
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
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance and condition is true.</exception>
	public Metadata WithIf<T>(bool condition, string key, Func<T?> valueFactory) {
		if (!condition) return this;
		ThrowIfLocked();
		_dictionary[key] = valueFactory();
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
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance and predicate returns true.</exception>
	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, T? value) {
		if (!predicate(this)) return this;
		ThrowIfLocked();
		_dictionary[key] = value;
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
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance and predicate returns true.</exception>
	public Metadata WithIf<T>(Func<Metadata, bool> predicate, string key, Func<T?> valueFactory) {
		if (!predicate(this)) return this;
		ThrowIfLocked();
		_dictionary[key] = valueFactory();
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
        if (!condition) return this;

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
        if (!condition) return this;

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
        if (!condition) return this;

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
        if (!predicate(this)) return this;

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
        if (!predicate(this)) return this;

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
        if (!predicate(this)) return this;

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
        if (!predicate(this)) return this;

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
        if (!condition) return this;

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
        if (!condition) return this;

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
        if (!predicate(this)) return this;

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
        if (!predicate(this)) return this;

        foreach (var key in keys)
            _dictionary.Remove(key);

        return this;
	}

    /// <summary>
    /// Applies a transformation function to the metadata.
    /// </summary>
    /// <param name="transform">The transformation function to apply.</param>
    /// <returns>This metadata instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance.</exception>
    public Metadata Transform(Action<Metadata> transform) {
        ThrowIfLocked();
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
        !condition ? this : transform(this);

    /// <summary>
	/// Applies a transformation function to the metadata only if a predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="transform">The transformation function to apply if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata TransformIf(Func<Metadata, bool> predicate, Func<Metadata, Metadata> transform) =>
        !predicate(this) ? this : transform(this);

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

	public static implicit operator Metadata(Dictionary<string, object?> dictionary) => new(dictionary);
}
