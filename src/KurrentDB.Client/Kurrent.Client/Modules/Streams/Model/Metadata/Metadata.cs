using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Kurrent.Client.Streams;

namespace Kurrent.Client;

/// <summary>
/// Represents a collection of metadata as key-value pairs with additional helper methods.
/// Note: This class is not thread-safe. Synchronization must be provided by the caller when accessed from multiple threads.
/// </summary>
[PublicAPI]
[DebuggerDisplay("{ToDebugString(prettyPrint: true)}")]
[JsonConverter(typeof(MetadataJsonConverter))]
public class Metadata : IEnumerable<KeyValuePair<string, object?>> {
	internal readonly Dictionary<string, object?> Dictionary;

	/// <summary>
	/// Initializes a new, empty instance of the Metadata class using ordinal string comparison.
	/// </summary>
	public Metadata() =>
		Dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the Metadata class from an existing metadata instance.
	/// </summary>
	/// <param name="metadata">The metadata to copy from.</param>
    public Metadata(Metadata metadata) =>
		Dictionary = new Dictionary<string, object?>(metadata.Dictionary, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Initializes a new instance of the Metadata class from a dictionary.
	/// </summary>
	/// <param name="items">The dictionary to copy from.</param>
    public Metadata(Dictionary<string, object?> items) {
	    Dictionary = new Dictionary<string, object?>(items, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Initializes a new instance of the Metadata class from a collection of key-value pairs.
	/// </summary>
	/// <param name="items">The collection of key-value pairs to copy from.</param>
    public Metadata(IEnumerable<KeyValuePair<string, object?>> items) {
		// Skip the dictionary constructor to avoid ambiguity
		Dictionary = items.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
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
		IsLocked = true;
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
        IsLocked ? throw new InvalidOperationException("Cannot modify locked metadata. Use CreateUnlockedCopy() to create a mutable copy.") : this;

    #region . IDictionary .

	/// <summary>
	/// Gets the number of elements in the collection.
	/// </summary>
	public int Count => Dictionary.Count;

	/// <summary>
	/// Gets a value indicating whether the dictionary is read-only.
	/// </summary>
	public bool IsReadOnly => false;

	/// <summary>
	/// Gets a value indicating whether this metadata instance is locked and cannot be modified directly.
	/// </summary>
	public bool IsLocked { get; private set; }

	/// <summary>
	/// Gets an collection that contains the keys in the dictionary.
	/// </summary>
	public ICollection<string> Keys => Dictionary.Keys;

	/// <summary>
	/// Gets an collection that contains the values in the dictionary.
	/// </summary>
	public ICollection<object?> Values => Dictionary.Values;

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key to look up or set.</param>
	/// <returns>The value associated with the specified key.</returns>
	/// <exception cref="KeyNotFoundException">The key does not exist in the collection (when getting).</exception>
	/// <exception cref="InvalidOperationException">Thrown when attempting to set a value on a locked metadata instance.</exception>
	public object? this[string key] {
		get => Dictionary[key];
		set {
			ThrowIfLocked();
			Dictionary[key] = value;
		}
	}

	/// <summary>
	/// Removes all keys and values from the dictionary.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when attempting to clear a locked metadata instance.</exception>
	public void Clear() {
		ThrowIfLocked();
		Dictionary.Clear();
	}

	/// <summary>
	/// Determines whether the dictionary contains the specified key.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
	public bool ContainsKey(string key) => Dictionary.ContainsKey(key);

	/// <summary>
	/// Removes the specified key from the dictionary.
	/// </summary>
	/// <param name="key">The key to remove.</param>
	/// <returns>true if the element is successfully removed; otherwise, false.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to remove from a locked metadata instance.</exception>
	public bool Remove(string key) {
		ThrowIfLocked();
		return Dictionary.Remove(key);
	}

	/// <summary>
	/// Returns an enumerator that iterates through the collection.
	/// </summary>
	/// <returns>An enumerator that can be used to iterate through the collection.</returns>
	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Dictionary.GetEnumerator();

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
		// check if the key exists
		if (!Dictionary.TryGetValue(key, out var obj)) {
			value = default;
			return false;
		}

		switch (obj) {
			// direct type match (most common case)
			case T typedValue:
				value = typedValue;
				return true;

			case JsonValue jsonValue:
				value = jsonValue.TryGetValue<T>(out var jsonTypedValue) ? jsonTypedValue : default;
				return true;

			// handle null case
			case null:
				value = default;
				return typeof(T).IsClass || Nullable.GetUnderlyingType(typeof(T)) is not null;

			// If the object is a string, try to convert it to type T, unless T is string itself
			case string stringValue: {
				// if T is string, we can directly assign
				if (typeof(T) == typeof(string)) {
					value = (T)(object)stringValue;
					return true;
				}

				stringValue = stringValue.Trim();

				var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

				// handle basic types with TypeConverter
				var converter = TypeDescriptor.GetConverter(targetType);
				if (converter.CanConvertFrom(typeof(string)))
					try {
						value = (T)converter.ConvertFromInvariantString(stringValue)!;
						return true;
					}
					catch {
						value = default;
						return false;
					}

                // handle enum types
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

		// if we got here, we couldn't convert
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

    /// <summary>
    /// Gets the value associated with the specified key as the specified type.
    /// Throws a KeyNotFoundException if the key is not found or if the value is null.
    /// </summary>
    /// <typeparam name="T">The type to get or convert to.</typeparam>
    /// <param name="key">The key to retrieve.</param>
    /// <returns>
    /// The value associated with the specified key cast to type T.
    /// </returns>
    public T GetRequired<T>(string key) =>
        TryGet<T>(key, out var typedValue) && typedValue is not null
            ? typedValue
            : throw new KeyNotFoundException($"Key '{key}' not found or value is null.");

	#endregion

	#region . Modification Methods .

	/// <summary>
	/// Evolves the value associated with the specified key by applying a transformation function.
	/// </summary>
	public Metadata EvolveValue<T, TE>(string key, Func<T?, TE> transform) {
		ThrowIfLocked();

		if (TryGet<T>(key, out var value))
			Dictionary[key] = transform(value);

		return this;
	}

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
		Dictionary[key] = value;
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
		Dictionary[key] = value;
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
		foreach (var item in items) Dictionary[item.Key] = item.Value;
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
		foreach (var item in items) Dictionary[item.Key] = item.Value;
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
		foreach (var item in items) Dictionary[item.Key] = item.Value;
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
		Dictionary.Remove(key);
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
		foreach (var key in keys) Dictionary.Remove(key);
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
		foreach (var key in keys) Dictionary.Remove(key);
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
		ThrowIfLocked();
		if (!condition) return this;
		Dictionary[key] = value;
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
		ThrowIfLocked();
		if (!condition) return this;
		Dictionary[key] = valueFactory();
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
		ThrowIfLocked();
		if (!predicate(this)) return this;
		Dictionary[key] = value;
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
		ThrowIfLocked();
		if (!predicate(this)) return this;
		Dictionary[key] = valueFactory();
		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The metadata containing key-value pairs to be added or updated if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	/// <exception cref="InvalidOperationException">Thrown when attempting to modify a locked metadata instance and condition is true.</exception>
	public Metadata WithManyIf(bool condition, Metadata items) {
		ThrowIfLocked();
		if (!condition) return this;
		foreach (var item in items)
			Dictionary[item.Key] = item.Value;

		return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, IDictionary<string, object?> items) {
		ThrowIfLocked();
		if (!condition) return this;
        foreach (var item in items)
            Dictionary[item.Key] = item.Value;

        return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="items">The key-value pairs to add or update if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(bool condition, params KeyValuePair<string, object?>[] items) {
		ThrowIfLocked();
		if (!condition) return this;
        foreach (var item in items)
            Dictionary[item.Key] = item.Value;

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
		ThrowIfLocked();
		if (!condition) return this;
        foreach (var item in itemsFactory())
            Dictionary[item.Key] = item.Value;

        return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The metadata containing key-value pairs to be added or updated if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, Metadata items) {
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var item in items)
            Dictionary[item.Key] = item.Value;

        return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs from a dictionary in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The dictionary containing key-value pairs to be added or updated if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, IDictionary<string, object?> items) {
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var item in items)
            Dictionary[item.Key] = item.Value;

        return this;
	}

	/// <summary>
	/// Adds or updates multiple key-value pairs in the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="items">The key-value pairs to add or update if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithManyIf(Func<Metadata, bool> predicate, params KeyValuePair<string, object?>[] items) {
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var item in items)
            Dictionary[item.Key] = item.Value;

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
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var item in itemsFactory())
            Dictionary[item.Key] = item.Value;

        return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="key">The key to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutIf(bool condition, string key) {
		ThrowIfLocked();
		if (condition) Dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="keys">The collection of keys to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(bool condition, IEnumerable<string> keys) {
		ThrowIfLocked();
		if (!condition) return this;
        foreach (var key in keys)
            Dictionary.Remove(key);

        return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if a condition is true.
	/// </summary>
	/// <param name="condition">The condition to evaluate.</param>
	/// <param name="keys">The keys to remove if the condition is true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(bool condition, params string[] keys) {
		ThrowIfLocked();
		if (!condition) return this;
        foreach (var key in keys)
            Dictionary.Remove(key);

        return this;
	}

	/// <summary>
	/// Removes the specified key from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="key">The key to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutIf(Func<Metadata, bool> predicate, string key) {
		ThrowIfLocked();
		if (predicate(this)) Dictionary.Remove(key);
		return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="keys">The collection of keys to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, IEnumerable<string> keys) {
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var key in keys)
            Dictionary.Remove(key);

        return this;
	}

	/// <summary>
	/// Removes multiple keys from the metadata only if the predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="keys">The keys to remove if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata WithoutManyIf(Func<Metadata, bool> predicate, params string[] keys) {
		ThrowIfLocked();
		if (!predicate(this)) return this;
        foreach (var key in keys)
            Dictionary.Remove(key);

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
	public Metadata TransformIf(bool condition, Func<Metadata, Metadata> transform) {
		ThrowIfLocked();
	    return !condition ? this : transform(this);
	}

	/// <summary>
	/// Applies a transformation function to the metadata only if a predicate evaluates to true.
	/// </summary>
	/// <param name="predicate">A function that tests this metadata instance and returns a boolean.</param>
	/// <param name="transform">The transformation function to apply if the predicate returns true.</param>
	/// <returns>This metadata instance for method chaining.</returns>
	public Metadata TransformIf(Func<Metadata, bool> predicate, Func<Metadata, Metadata> transform) {
		ThrowIfLocked();
		return !predicate(this) ? this : transform(this);
	}

	/// <summary>
	/// Creates a new Metadata instance where all values are converted to their string representation
	/// using appropriate formatting for common types (ISO dates, etc.).
	/// </summary>
	/// <returns>A new Metadata instance with all values as formatted strings.</returns>
	public Metadata Stringify() {
		return new Metadata()
			.WithMany(this.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, ConvertValueToString(kvp.Value))));

		static string? ConvertValueToString(object? value) =>
			value switch {
				null                          => null,
				DateTime dateTime             => dateTime.ToString("O"),
				DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
				DateOnly dateOnly             => dateOnly.ToString("O"),
				TimeOnly timeOnly             => timeOnly.ToString("O"),
				TimeSpan timeSpan             => timeSpan.ToString("c"),
				Guid guid                     => guid.ToString("D"),
				int int32                     => int32.ToString(CultureInfo.InvariantCulture),
				long int64                    => int64.ToString(CultureInfo.InvariantCulture),
				decimal dec                   => dec.ToString(CultureInfo.InvariantCulture),
				double dbl                    => dbl.ToString(CultureInfo.InvariantCulture),
				float flt                     => flt.ToString(CultureInfo.InvariantCulture),
				byte b                        => b.ToString(CultureInfo.InvariantCulture),
				char c                        => c.ToString(CultureInfo.InvariantCulture),
				byte[] bytes                  => Convert.ToBase64String(bytes),
				Memory<byte> memory           => Convert.ToBase64String(memory.Span),
				ReadOnlyMemory<byte> memory   => Convert.ToBase64String(memory.Span),
				string str                    => str,
				IEnumerable enumerable        => string.Join(", ", enumerable.Cast<object?>().Select(ConvertValueToString)),
				_                             => value.ToString()
			};
	}

    #endregion

	/// <summary>
	/// Returns a string representation of the metadata suitable for debugging.
	/// </summary>
	public override string ToString() =>
		$"Metadata ({Count} items): {string.Join("\n", Dictionary.Select(p => $"  {p.Key} = {p.Value ?? "null"}"))}";

	/// <summary>
	/// Creates a debug view.
	/// </summary>
	string ToDebugString(bool prettyPrint = false) =>
		!prettyPrint ? ToString() : $"Metadata:\n{string.Join("\n", Dictionary.Select(p => $"  {p.Key} = {p.Value ?? "null"} {p.Value?.GetType().Name}"))}";

	public static implicit operator Metadata(Dictionary<string, object?> dictionary) => new(dictionary);
}
