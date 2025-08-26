using System.Collections;
using System.Collections.Concurrent;

namespace Kurrent.Client;

/// <summary>
/// Represents a thread-safe, bidirectional collection of keys and values.
/// Provides fast, lock-free reads and enumerations where possible,
/// while ensuring atomicity for write operations.
/// </summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
class ConcurrentBidirectionalDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDisposable
    where TKey : notnull where TValue : notnull {
    readonly ConcurrentDictionary<TKey, TValue> _forward;
    readonly IEqualityComparer<TKey>            _keyComparer;
    readonly ReaderWriterLockSlim               _lock = new(LockRecursionPolicy.NoRecursion);
    readonly ConcurrentDictionary<TValue, TKey> _reverse;
    readonly IEqualityComparer<TValue>          _valueComparer;

    public ConcurrentBidirectionalDictionary()
        : this(EqualityComparer<TKey>.Default, EqualityComparer<TValue>.Default) { }

    public ConcurrentBidirectionalDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) {
        _forward = new(_keyComparer   = keyComparer);
        _reverse = new(_valueComparer = valueComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentBidirectionalDictionary{TKey,TValue}"/> class
    /// that is empty, has the specified concurrency level, has the specified initial capacity, and
    /// uses the default equality comparer for the key type.
    /// </summary>
    /// <remarks>
    /// For performance-critical scenarios, it is recommended to provide an initial capacity
    /// to avoid resizing operations.
    /// </remarks>
    /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
    /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
    public ConcurrentBidirectionalDictionary(int concurrencyLevel, int capacity)
        : this(
            concurrencyLevel, capacity, EqualityComparer<TKey>.Default,
            EqualityComparer<TValue>.Default
        ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentBidirectionalDictionary{TKey,TValue}"/> class
    /// that is empty, has the specified concurrency level and capacity, and uses the specified
    /// <see cref="IEqualityComparer{TKey}"/>.
    /// </summary>
    /// <param name="concurrencyLevel">The estimated number of threads that will update the dictionary concurrently.</param>
    /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
    /// <param name="keyComparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys.</param>
    /// <param name="valueComparer">The <see cref="IEqualityComparer{TValue}"/> implementation to use when comparing values.</param>
    public ConcurrentBidirectionalDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) {
        _forward = new(concurrencyLevel, capacity, _keyComparer   = keyComparer);
        _reverse = new(concurrencyLevel, capacity, _valueComparer = valueComparer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentBidirectionalDictionary{TKey,TValue}"/> class that contains
    /// elements copied from the specified <see cref="ConcurrentBidirectionalDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <param name="other">The <see cref="ConcurrentBidirectionalDictionary{TKey,TValue}"/> whose elements are copied to the new dictionary.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="other"/> is null.</exception>
    public ConcurrentBidirectionalDictionary(ConcurrentBidirectionalDictionary<TKey, TValue> other) {
        ArgumentNullException.ThrowIfNull(other);

        _forward = new(_keyComparer   = other._keyComparer);
        _reverse = new(_valueComparer = other._valueComparer);

        // Copy all entries with TryAdd which handles the bidirectional mapping atomically
        foreach (var pair in other._forward)
            TryAdd(pair.Key, pair.Value);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <remarks>
    /// Getting a value is typically lock-free, while setting a value requires a write lock to ensure atomicity.
    /// </remarks>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">The property is retrieved and key does not exist in the collection.</exception>
    /// <exception cref="ArgumentException">The property is set and the value already exists mapped to a different key.</exception>
    public TValue this[TKey key] {
        get => _forward[key];
        set => Set(key, value);
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <remarks>
    /// This operation requires a write lock to ensure atomicity.
    /// </remarks>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <exception cref="ArgumentException">An element with the same key or value already exists.</exception>
    public void Add(TKey key, TValue value) {
        if (TryAdd(key, value)) return;

        if (_forward.ContainsKey(key))
            throw new ArgumentException("An element with the same key already exists.", nameof(key));

        if (_reverse.ContainsKey(value))
            throw new ArgumentException("An element with the same value already exists.", nameof(value));

        throw new ArgumentException("An element with the same key or value already exists.");
    }

    ///<inheritdoc />
    bool IDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <remarks>
    /// This operation is typically lock-free.
    /// </remarks>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(TKey key, out TValue value) => _forward.TryGetValue(key, out value!);

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <remarks>
    /// This operation is typically lock-free.
    /// </remarks>
    /// <param name="key">The key to locate.</param>
    /// <returns><c>true</c> if the dictionary contains an element with the specified key; otherwise, <c>false</c>.</returns>
    public bool ContainsKey(TKey key) => _forward.ContainsKey(key);

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    /// <remarks>
    /// This operation requires a write lock.
    /// </remarks>
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
    /// </summary>
    /// <remarks>
    /// Accessing the count on <see cref="ConcurrentDictionary{TKey,TValue}"/> is typically lock-free but might represent a slightly stale value
    /// if accessed concurrently with write operations not yet fully completed across both internal dictionaries.
    /// </remarks>
    public int Count => _forward.Count;

    ///<inheritdoc />
    public ICollection<TKey> Keys => _forward.Keys;

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    /// <remarks>
    /// The collection reflects the state of the dictionary at the point it's retrieved.
    /// Value uniqueness is enforced by the dictionary.
    /// </remarks>
    public ICollection<TValue> Values => _forward.Values;

    ///<inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _forward.GetEnumerator();

    ///<inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    ///<inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    ///<inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    /// <summary>
    /// Sets the specified key and value pair, updating the dictionaries accordingly.
    /// </summary>
    /// <remarks>
    /// This operation requires a write lock.
    /// </remarks>
    /// <param name="key">The key for the entry to add or update.</param>
    /// <param name="value">The value associated with the specified key.</param>
    /// <exception cref="ArgumentException">Thrown if the value is already associated with a different key.</exception>
    void Set(TKey key, TValue value) {
        _lock.EnterWriteLock();
        try {
            // Check if the value is already mapped to a different key
            if (_reverse.TryGetValue(value, out var existingKey) && !_keyComparer.Equals(existingKey, key))
                throw new ArgumentException($"Value '{value}' already exists for key '{existingKey}'. Cannot map it to key '{key}'.");

            // Check if the key already exists and get its current value
            if (_forward.TryGetValue(key, out var oldValue) && !_valueComparer.Equals(oldValue, value))
                // If the key exists but has a different value, remove the old reverse mapping
                _reverse.TryRemove(oldValue, out _);

            // Update the mappings
            _forward[key]   = value;
            _reverse[value] = key;
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the key associated with the specified value.
    /// </summary>
    /// <remarks>
    /// This operation is typically lock-free.
    /// </remarks>
    /// <param name="value">The value to locate.</param>
    /// <returns>The key associated with the value.</returns>
    /// <exception cref="KeyNotFoundException">The value does not exist in the collection.</exception>
    public TKey GetKeyByValue(TValue value) => _reverse[value];

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <remarks>
    /// This operation is atomic and requires a write lock.
    /// </remarks>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><c>true</c> if the key/value pair was added successfully; otherwise, <c>false</c>.</returns>
    public bool TryAdd(TKey key, TValue value) {
        _lock.EnterWriteLock();
        try {
            // Check for existence within the lock
            if (_forward.ContainsKey(key) || _reverse.ContainsKey(value))
                return false;

            // Since we're under a write lock and already verified keys don't exist,
            // these operations will always succeed
            _forward[key]   = value;
            _reverse[value] = key;

            return true;
        }
        finally {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Attempts to remove the key and its associated value from the dictionary.
    /// </summary>
    /// <remarks>
    /// This operation requires a write lock.
    /// </remarks>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TKey key) {
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
    /// Attempts to remove the value and its associated key from the dictionary.
    /// </summary>
    /// <remarks>
    /// This operation requires a write lock.
    /// </remarks>
    /// <param name="value">The value of the element to remove.</param>
    /// <returns><c>true</c> if the value was found and removed; otherwise, <c>false</c>.</returns>
    public bool RemoveByValue(TValue value) {
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
    /// Attempts to get the key associated with the specified value.
    /// </summary>
    /// <remarks>
    /// This operation is typically lock-free.
    /// </remarks>
    /// <param name="value">The value of the key to get.</param>
    /// <param name="key">When this method returns, contains the key associated with the specified value, if the value is found; otherwise, the default value for the type of the key parameter.</param>
    /// <returns><c>true</c> if the value was found; otherwise, <c>false</c>.</returns>
    public bool TryGetKey(TValue value, out TKey key) => _reverse.TryGetValue(value, out key!);

    /// <summary>
    /// Determines whether the dictionary contains the specified value.
    /// </summary>
    /// <remarks>
    /// This operation is typically lock-free.
    /// </remarks>
    /// <param name="value">The value to locate.</param>
    /// <returns><c>true</c> if the dictionary contains an element with the specified value; otherwise, <c>false</c>.</returns>
    public bool ContainsValue(TValue value) => _reverse.ContainsKey(value);

    #region . explicit interface implementations .

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        _forward.TryGetValue(item.Key, out var value) && _valueComparer.Equals(value, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) {
        _lock.EnterWriteLock();
        try {
            // Only remove if the key exists AND is mapped to the specified value
            // Using Contains prior check might be racy, check inside the lock more robustly
            // Note: ConcurrentDictionary doesn't have a direct atomic TryRemove(KeyValuePair) like Dictionary.
            // We need to check the value before removing.
            if (_forward.TryGetValue(item.Key, out var currentValue) && _valueComparer.Equals(currentValue, item.Value)) {
                // Now attempt to remove both under the lock
                // We know the value matches, just remove by key
                if (_forward.TryRemove(item.Key, out _)) {
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
                if (i >= array.Length)
                    throw new ArgumentException("Destination array was not large enough. This might be due to concurrent additions.");

                array[i++] = kvp;
            }
        }
        finally {
            _lock.ExitReadLock(); // Release read lock
        }
    }

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    #endregion

    #region . IDisposable .

    bool _disposed;

    protected virtual void Dispose(bool disposing) {
        if (_disposed) return;

        if (disposing)
            _lock?.Dispose();

        _disposed = true;
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
