using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace KurrentDB.Client;

/// <summary>
/// Represents a thread-safe, bidirectional collection of keys and values.
/// Provides fast, lock-free reads and enumerations where possible,
/// while ensuring atomicity for write operations.
/// </summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
public class ConcurrentBidirectionalDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
	where TKey : notnull where TValue : notnull {

	readonly ConcurrentDictionary<TKey, TValue> _forward;
	readonly IEqualityComparer<TKey>            _keyComparer;
	readonly ReaderWriterLockSlim               _lock = new(LockRecursionPolicy.NoRecursion);
	readonly ConcurrentDictionary<TValue, TKey> _reverse;
	readonly IEqualityComparer<TValue>          _valueComparer;

	public ConcurrentBidirectionalDictionary()
		: this(EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

	public ConcurrentBidirectionalDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) {
		_keyComparer   = keyComparer;
		_valueComparer = valueComparer;
		_forward       = new ConcurrentDictionary<TKey, TValue>(_keyComparer);
		_reverse       = new ConcurrentDictionary<TValue, TKey>(_valueComparer);
	}

	// public ConcurrentBidirectionalDictionary(int concurrencyLevel, int capacity)
	// 	: this(concurrencyLevel, capacity, EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }
	//
	// public ConcurrentBidirectionalDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) {
	// 	_keyComparer   = keyComparer;
	// 	_valueComparer = valueComparer;
	// 	_forward       = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, _keyComparer);
	// 	_reverse       = new ConcurrentDictionary<TValue, TKey>(concurrencyLevel, capacity, _valueComparer);
	// }

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// Setting a value requires a write lock to ensure atomicity across both internal dictionaries.
	/// Getting a value is typically lock-free.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <exception cref="ArgumentNullException">key is null.</exception>
	/// <exception cref="KeyNotFoundException">The property is retrieved and key does not exist in the collection.</exception>
	/// <exception cref="ArgumentException">The property is set and the value already exists mapped to a different key.</exception>
	public TValue this[TKey key] {
		// Note: IReadOnlyDictionary only requires a getter. IDictionary requires getter+setter.
		// The public getter satisfies IReadOnlyDictionary.
		get => _forward[key]; // Use ConcurrentDictionary's getter (throws KeyNotFoundException)
		set => Set(key, value);
	}

	/// <summary>
	/// Attempts to add the specified key and value. Requires a write lock.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	/// <exception cref="ArgumentNullException">key or value is null.</exception>
	/// <exception cref="ArgumentException">key or value already exists.</exception>
	public void Add(TKey key, TValue value) {
		if (TryAdd(key, value)) return;

		// Determine specific reason for failure for better exception message
		if (_forward.ContainsKey(key))
			throw new ArgumentException("Key already exists.", nameof(key));

		if (_reverse.ContainsKey(value))
			throw new ArgumentException("Value already exists.", nameof(value));

		// If neither contains, it means a race condition occurred between check and add attempt inside TryAdd.
		// Throwing a generic ArgumentException is reasonable here.
		throw new ArgumentException("An item with the same key or value already exists.");
	}

	// IDictionary explicit implementation uses the public Remove
	bool IDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);

	/// <summary>
	/// Attempts to get the value associated with the specified key. Typically lock-free.
	/// </summary>
	/// <param name="key">The key of the value to get.</param>
	/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
	/// <returns>true if the key was found; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">key is null.</exception>
	public bool TryGetValue(TKey key, out TValue value) =>
	// public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
		// ConcurrentDictionary.TryGetValue is thread-safe and typically lock-free
		// Satisfies both IDictionary and IReadOnlyDictionary
		_forward.TryGetValue(key, out value!);

	// public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
	// 	// ConcurrentDictionary.TryGetValue is thread-safe and typically lock-free
	// 	// Satisfies both IDictionary and IReadOnlyDictionary
	// 	_forward.TryGetValue(key, out value);

	/// <summary>
	/// Determines whether the dictionary contains the specified key. Typically lock-free.
	/// </summary>
	/// <param name="key">The key to locate.</param>
	/// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">key is null.</exception>
	public bool ContainsKey(TKey key) =>
		// ConcurrentDictionary.ContainsKey is thread-safe and typically lock-free
		// Satisfies both IDictionary and IReadOnlyDictionary
		_forward.ContainsKey(key);

	/// <summary>
	/// Removes all keys and values. Requires a write lock.
	/// </summary>
	public void Clear() {
		_lock.EnterWriteLock();
		try {
			// Need to clear both under the same lock
			_forward.Clear();
			_reverse.Clear();
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Gets the number of key/value pairs contained in the dictionary.
	/// Accessing the count on ConcurrentDictionary is typically lock-free but might represent a slightly stale value
	/// if accessed concurrently with write operations not yet fully completed across both internal dictionaries.
	/// </summary>
	public int Count => _forward.Count; // Satisfies both ICollection<> and IReadOnlyCollection<>

	// --- IDictionary & IReadOnlyDictionary Properties ---

	/// <summary>
	/// Gets a collection containing the keys. Implements IDictionary.Keys.
	/// Does not allocate a new list on access. The collection reflects the state of the dictionary at the point it's retrieved.
	/// </summary>
	public ICollection<TKey> Keys => _forward.Keys;

	/// <summary>
	/// Gets a collection containing the values. Implements IDictionary.Values.
	/// Does not allocate a new list on access. The collection reflects the state of the dictionary at the point it's retrieved.
	/// Note: Value uniqueness is enforced by the dictionary, but ConcurrentDictionary.Values itself doesn't guarantee uniqueness checks on enumeration if the underlying dictionary changes concurrently.
	/// </summary>
	public ICollection<TValue> Values => _forward.Values; // Using _forward's values, consistent with Keys/Enumerator

	// --- Enumerators ---

	/// <summary>
	/// Returns an enumerator that iterates through the key/value pairs. Implements IDictionary and IEnumerable<>.
	/// Does not allocate a new list. The enumeration represents a moment-in-time snapshot.
	/// </summary>
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
		// Returns the enumerator directly from ConcurrentDictionary. No ToList().
		_forward.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	// --- Explicit Interface Implementations for IReadOnlyDictionary ---
	// Needed because IDictionary defines Keys/Values returning ICollection<T>,
	// while IReadOnlyDictionary defines them returning IEnumerable<T>.
	// The public ICollection<T> properties satisfy the IEnumerable<T> contract,
	// but explicit implementation clarifies intent for the compiler/runtime.

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

	// Explicit implementation for IReadOnlyDictionary indexer getter (optional, public getter suffices)
	// TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key] => this[key];

	void Set(TKey key, TValue value) {
		if (key is null)
			throw new ArgumentNullException(nameof(key));

		if (value is null)
			throw new ArgumentNullException(nameof(value));

		_lock.EnterWriteLock();
		try {
			// Check if the new value conflicts with an existing mapping
			if (_reverse.TryGetValue(value, out var existingKey) && !_keyComparer.Equals(existingKey, key))
				throw new ArgumentException($"Value '{value}' already exists for key '{existingKey}'. Cannot map it to key '{key}'.");

			// Add/update the forward dictionary and get the *old* value if it existed
			TValue? oldValue    = default;
			var     hadOldValue = false;

			_forward.AddOrUpdate(
				key,
				// Add factory: maps value to key in reverse
				k => {
					// We might have added to _reverse speculatively before, ensure consistency
					// Or, more simply, just add/update reverse map *after* forward map is settled
					// We'll handle the reverse update below after AddOrUpdate completes
					return value;
				},
				// Update factory: potentially removes old reverse mapping
				(k, existingValue) => {
					oldValue    = existingValue;
					hadOldValue = true;
					return value; // Return the new value
				}
			);

			// If the key previously existed and had a different value, remove the old reverse mapping
			#if NET48
			if (hadOldValue && !_valueComparer.Equals(oldValue!, value))
			#else
			if (hadOldValue && !_valueComparer.Equals(oldValue, value))
			#endif
				// oldValue cannot be null here if hadOldValue is true, but compiler needs hint
				if (oldValue != null)
					_reverse.TryRemove(oldValue, out _);

			// Ensure the reverse mapping is correct for the new value
			_reverse[value] = key; // Add or update reverse mapping
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Gets the key associated with the specified value. Typically lock-free.
	/// </summary>
	/// <param name="value">The value to locate.</param>
	/// <returns>The key associated with the value.</returns>
	/// <exception cref="ArgumentNullException">value is null.</exception>
	/// <exception cref="KeyNotFoundException">The value does not exist in the collection.</exception>
	public TKey GetKeyByValue(TValue value) {
		if (value == null) throw new ArgumentNullException(nameof(value));

		// Use ConcurrentDictionary's indexer (throws KeyNotFoundException if value isn't found)
		return _reverse[value];
	}

	/// <summary>
	/// Attempts to add the specified key and value atomically. Requires a write lock.
	/// </summary>
	/// <param name="key">The key of the element to add.</param>
	/// <param name="value">The value of the element to add.</param>
	/// <returns>true if the key/value pair was added successfully; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">key or value is null.</exception>
	public bool TryAdd(TKey key, TValue value) {
		if (key == null) throw new ArgumentNullException(nameof(key));
		if (value == null) throw new ArgumentNullException(nameof(value));

		_lock.EnterWriteLock();
		try {
			// Check for existence *within the lock* to ensure atomicity
			if (_forward.ContainsKey(key) || _reverse.ContainsKey(value)) return false;

			// Try adding to forward. If it fails unexpectedly (e.g., race condition if check was outside lock), bail out.
			if (!_forward.TryAdd(key, value))
				// This shouldn't happen if checks are inside the lock, but defensively handle it.
				return false;

			// Try adding to reverse. If it fails, we MUST roll back the forward add.
			if (!_reverse.TryAdd(value, key)) {
				// Rollback: Remove the key from the forward dictionary.
				// We need to use TryRemove that matches the value we just added.

				#if NET48
				_forward.TryRemove(key, out _);
				#else
				_forward.TryRemove(KeyValuePair.Create(key, value));
				#endif

				return false;
			}

			// Both additions succeeded
			return true;
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Attempts to remove the key and its associated value. Requires a write lock.
	/// </summary>
	/// <param name="key">The key of the element to remove.</param>
	/// <returns>true if the key was found and removed; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">key is null.</exception>
	public bool Remove(TKey key) {
		if (key == null) throw new ArgumentNullException(nameof(key));

		_lock.EnterWriteLock();
		try {
			if (_forward.TryRemove(key, out var value)) {
				// Ensure the reverse mapping is also removed.
				// value cannot be null if TryRemove succeeded
				_reverse.TryRemove(value, out _);
				return true;
			}

			return false;
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Attempts to remove the value and its associated key. Requires a write lock.
	/// </summary>
	/// <param name="value">The value of the element to remove.</param>
	/// <returns>true if the value was found and removed; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">value is null.</exception>
	public bool RemoveByValue(TValue value) {
		if (value == null) throw new ArgumentNullException(nameof(value));

		_lock.EnterWriteLock();
		try {
			if (_reverse.TryRemove(value, out var key)) {
				// Ensure the forward mapping is also removed.
				// key cannot be null if TryRemove succeeded
				_forward.TryRemove(key, out _);
				return true;
			}

			return false;
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Attempts to get the key associated with the specified value. Typically lock-free.
	/// </summary>
	/// <param name="value">The value of the key to get.</param>
	/// <param name="key">When this method returns, contains the key associated with the specified value, if the value is found; otherwise, the default value for the type of the key parameter.</param>
	/// <returns>true if the value was found; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">value is null.</exception>
	public bool TryGetKey(TValue value, out TKey key) {
		if (value == null) throw new ArgumentNullException(nameof(value));

		// ConcurrentDictionary.TryGetValue is thread-safe and typically lock-free
		return _reverse.TryGetValue(value, out key!);
	}

	// public bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key) {
	// 	if (value == null) throw new ArgumentNullException(nameof(value));
	//
	// 	// ConcurrentDictionary.TryGetValue is thread-safe and typically lock-free
	// 	return _reverse.TryGetValue(value, out key);
	// }

	/// <summary>
	/// Determines whether the dictionary contains the specified value. Typically lock-free.
	/// </summary>
	/// <param name="value">The value to locate.</param>
	/// <returns>true if the dictionary contains an element with the specified value; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">value is null.</exception>
	public bool ContainsValue(TValue value) {
		if (value == null) throw new ArgumentNullException(nameof(value));

		// ConcurrentDictionary.ContainsKey is thread-safe and typically lock-free
		return _reverse.ContainsKey(value);
	}

	#region Explicit Interface Implementations (ICollection<KeyValuePair<TKey, TValue>>)

	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		=> Add(item.Key, item.Value); // Use the public Add method

	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
		// Check if the key exists and maps to the specific value. Typically lock-free.
		_forward.TryGetValue(item.Key, out var value) && _valueComparer.Equals(value, item.Value);

	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) {
		if (item.Key == null)
			throw new ArgumentNullException(nameof(item), "Key cannot be null.");

		if (item.Value == null)
			throw new ArgumentNullException(nameof(item), "Value cannot be null.");

		_lock.EnterWriteLock();
		try {
			// Only remove if the key exists AND is mapped to the specified value
			// Using Contains prior check might be racy, check inside the lock more robustly
			// Note: ConcurrentDictionary doesn't have a direct atomic TryRemove(KeyValuePair) like Dictionary.
			// We need to check the value before removing.
			if (_forward.TryGetValue(item.Key, out var currentValue) && _valueComparer.Equals(currentValue, item.Value)) {
				// Now attempt to remove both under the lock
				if (_forward.TryRemove(item.Key, out _)) // We know the value matches, just remove by key
				{
					// Ensure reverse is also removed
					_reverse.TryRemove(item.Value, out _);
					return true;
				}
				else {
					// This case should be rare if TryGetValue succeeded just before,
					// implies another thread removed it between TryGetValue and TryRemove inside the lock.
					return false;
				}
			}

			return false; // Key wasn't found or mapped to a different value
		}
		finally {
			_lock.ExitWriteLock();
		}
	}

	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
		// ConcurrentDictionary doesn't directly support CopyTo like this atomically safe w.r.t reverse dictionary.
		// We need to acquire a read lock for a consistent snapshot of the forward dictionary pairs.
		_lock.EnterReadLock();
		try {
			// Use Count property which might be slightly stale, but is best effort without full lock
			// Re-check count inside lock for accuracy if needed, but forward.Count is safe here
			if (array.Length - arrayIndex < _forward.Count) throw new ArgumentException("Destination array is not large enough.");

			var i = arrayIndex;
			// Iterate over the forward dictionary (which is safe for concurrent reads)
			foreach (var kvp in _forward) {
				// Check bounds defensively in case count was stale and dictionary grew
				if (i >= array.Length) throw new ArgumentException("Destination array was not large enough. This might be due to concurrent additions.");

				array[i++] = kvp;
			}
		}
		finally {
			_lock.ExitReadLock(); // Release read lock
		}
	}

	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

	#endregion

	#region IDisposable Implementation

	bool _disposed;

	protected virtual void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing)
				// Dispose managed state (managed objects).
				_lock?.Dispose();

			_disposed = true;
		}
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~ConcurrentBidirectionalDictionary() {
		Dispose(false);
	}

	#endregion
}
