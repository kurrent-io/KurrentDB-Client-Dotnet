namespace Kurrent.Client.Tests.Internal;

public class ConcurrentBidirectionalDictionaryTests : IDisposable {
    readonly ConcurrentBidirectionalDictionary<string, int> _sut = new();

    public void Dispose() => _sut.Dispose();

    [Test]
    public void constructor_with_capacity_and_concurrency_should_initialize_correctly() {
        // Arrange
        var concurrencyLevel = 2;
        var capacity         = 100;

        // Act
        using var sut = new ConcurrentBidirectionalDictionary<string, int>(concurrencyLevel, capacity);
        sut.Add("one", 1);

        // Assert
        sut.Count.ShouldBe(1);
        sut.ContainsKey("one").ShouldBeTrue();
    }

    [Test]
    public void add_single_item_should_succeed_and_be_retrievable() {
        // Arrange
        var key   = "one";
        var value = 1;

        // Act
        _sut.Add(key, value);

        // Assert
        _sut.Count.ShouldBe(1);
        _sut.ContainsKey(key).ShouldBeTrue();
        _sut.ContainsValue(value).ShouldBeTrue();

        _sut.TryGetValue(key, out var retrievedValue).ShouldBeTrue();
        retrievedValue.ShouldBe(value);

        _sut.TryGetKey(value, out var retrievedKey).ShouldBeTrue();
        retrievedKey.ShouldBe(key);

        _sut[key].ShouldBe(value);
        _sut.GetKeyByValue(value).ShouldBe(key);
    }

    [Test]
    public void add_multiple_items_should_succeed_and_be_retrievable() {
        // Arrange & Act
        _sut.Add("one", 1);
        _sut.Add("two", 2);
        _sut.Add("three", 3);

        // Assert
        _sut.Count.ShouldBe(3);
        _sut["one"].ShouldBe(1);
        _sut.GetKeyByValue(1).ShouldBe("one");
        _sut["two"].ShouldBe(2);
        _sut.GetKeyByValue(2).ShouldBe("two");
        _sut["three"].ShouldBe(3);
        _sut.GetKeyByValue(3).ShouldBe("three");
    }

    [Test]
    public void try_add_new_item_should_succeed_and_return_true() {
        // Arrange
        var key   = "one";
        var value = 1;

        // Act
        var result = _sut.TryAdd(key, value);

        // Assert
        result.ShouldBeTrue();
        _sut.Count.ShouldBe(1);
        _sut[key].ShouldBe(value);
        _sut.GetKeyByValue(value).ShouldBe(key);
    }

    [Test]
    public void remove_existing_item_by_key_should_succeed_and_remove_both_mappings() {
        // Arrange
        _sut.Add("one", 1);
        _sut.Add("two", 2);

        // Act
        var result = _sut.Remove("one");

        // Assert
        result.ShouldBeTrue();
        _sut.Count.ShouldBe(1);
        _sut.ContainsKey("one").ShouldBeFalse();
        _sut.ContainsValue(1).ShouldBeFalse();
        _sut.ContainsKey("two").ShouldBeTrue(); // Ensure other item remains
        _sut.ContainsValue(2).ShouldBeTrue();
    }

    [Test]
    public void remove_existing_item_by_value_should_succeed_and_remove_both_mappings() {
        // Arrange
        _sut.Add("one", 1);
        _sut.Add("two", 2);

        // Act
        var result = _sut.RemoveByValue(1);

        // Assert
        result.ShouldBeTrue();
        _sut.Count.ShouldBe(1);
        _sut.ContainsKey("one").ShouldBeFalse();
        _sut.ContainsValue(1).ShouldBeFalse();
        _sut.ContainsKey("two").ShouldBeTrue(); // Ensure other item remains
        _sut.ContainsValue(2).ShouldBeTrue();
    }

    [Test]
    public void remove_non_existing_item_by_key_should_return_false() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var result = _sut.Remove("two");

        // Assert
        result.ShouldBeFalse();
        _sut.Count.ShouldBe(1);
    }

    [Test]
    public void remove_by_value_non_existing_item_should_return_false() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var result = _sut.RemoveByValue(2);

        // Assert
        result.ShouldBeFalse();
        _sut.Count.ShouldBe(1);
    }

    [Test]
    public void indexer_set_add_new_item_should_succeed() {
        // Arrange
        var key   = "ten";
        var value = 10;

        // Act
        _sut[key] = value;

        // Assert
        _sut.Count.ShouldBe(1);
        _sut[key].ShouldBe(value);
        _sut.GetKeyByValue(value).ShouldBe(key);
    }

    [Test]
    public void indexer_set_update_existing_item_should_update_both_mappings() {
        // Arrange
        _sut.Add("ten", 10);
        _sut.Add("twenty", 20);
        var newKey   = "ten";
        var newValue = 11; // New value for existing key "ten"

        // Act
        _sut[newKey] = newValue;

        // Assert
        _sut.Count.ShouldBe(2);                        // Count remains the same
        _sut[newKey].ShouldBe(newValue);               // Forward map updated
        _sut.ContainsValue(10).ShouldBeFalse();        // Old value removed from reverse map
        _sut.GetKeyByValue(newValue).ShouldBe(newKey); // New value added to reverse map
        _sut.ContainsKey("twenty").ShouldBeTrue();     // Other pair unaffected
        _sut.GetKeyByValue(20).ShouldBe("twenty");
    }

    [Test]
    public void indexer_set_update_existing_item_with_value_already_mapped_should_throw_argument_exception() {
        // Arrange
        _sut.Add("ten", 10);
        _sut.Add("twenty", 20);

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut["ten"] = 20) // Try map "ten" to 20, which is already used by "twenty"
            .Message.ShouldContain("Value '20' already exists for key 'twenty'.");

        // Verify state hasn't changed inconsistently
        _sut.Count.ShouldBe(2);
        _sut["ten"].ShouldBe(10);
        _sut.GetKeyByValue(10).ShouldBe("ten");
        _sut["twenty"].ShouldBe(20);
        _sut.GetKeyByValue(20).ShouldBe("twenty");
    }

    [Test]
    public void indexer_get_existing_item_should_return_value() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var value = _sut["one"];

        // Assert
        value.ShouldBe(1);
    }

    [Test]
    public void indexer_get_non_existing_item_should_throw_key_not_found_exception() {
        // Arrange & Act & Assert
        Should.Throw<KeyNotFoundException>(() => _sut["one"]);
    }

    [Test]
    public void get_key_by_value_existing_item_should_return_key() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var key = _sut.GetKeyByValue(1);

        // Assert
        key.ShouldBe("one");
    }

    [Test]
    public void get_key_by_value_non_existing_item_should_throw_key_not_found_exception() {
        // Arrange & Act & Assert
        Should.Throw<KeyNotFoundException>(() => _sut.GetKeyByValue(1));
    }

    [Test]
    public void add_duplicate_key_should_throw_argument_exception() {
        // Arrange
        _sut.Add("one", 1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.Add("one", 2)).ParamName.ShouldBe("key");

        // Verify state
        _sut.Count.ShouldBe(1);
        _sut["one"].ShouldBe(1);
        _sut.GetKeyByValue(1).ShouldBe("one");
    }

    [Test]
    public void add_duplicate_value_should_throw_argument_exception() {
        // Arrange
        _sut.Add("one", 1);

        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.Add("two", 1)).ParamName.ShouldBe("value");

        // Verify state
        _sut.Count.ShouldBe(1);
        _sut["one"].ShouldBe(1);
        _sut.GetKeyByValue(1).ShouldBe("one");
    }

    [Test]
    public void try_add_duplicate_key_should_return_false() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var result = _sut.TryAdd("one", 2);

        // Assert
        result.ShouldBeFalse();
        _sut.Count.ShouldBe(1);
        _sut["one"].ShouldBe(1); // Original value remains
    }

    [Test]
    public void try_add_duplicate_value_should_return_false() {
        // Arrange
        _sut.Add("one", 1);

        // Act
        var result = _sut.TryAdd("two", 1);

        // Assert
        result.ShouldBeFalse();
        _sut.Count.ShouldBe(1);
        _sut["one"].ShouldBe(1); // Original key remains
    }

    // Test for adding null key is removed as it's now enforced by 'notnull' constraint at compile time.

    [Test]
    public void contains_key_existing_key_should_return_true() {
        _sut.Add("a", 1);
        _sut.ContainsKey("a").ShouldBeTrue();
    }

    [Test]
    public void contains_key_non_existing_key_should_return_false() {
        _sut.ContainsKey("a").ShouldBeFalse();
    }

    [Test]
    public void contains_value_existing_value_should_return_true() {
        _sut.Add("a", 1);
        _sut.ContainsValue(1).ShouldBeTrue();
    }

    [Test]
    public void contains_value_non_existing_value_should_return_false() {
        _sut.ContainsValue(1).ShouldBeFalse();
    }

    [Test]
    public void try_get_value_existing_key_should_return_true_and_value() {
        _sut.Add("a", 1);
        var result = _sut.TryGetValue("a", out var value);
        result.ShouldBeTrue();
        value.ShouldBe(1);
    }

    [Test]
    public void try_get_value_non_existing_key_should_return_false_and_default_value() {
        var result = _sut.TryGetValue("a", out var value);
        result.ShouldBeFalse();
        value.ShouldBe(default(int)); // 0 for int
    }

    [Test]
    public void try_get_key_existing_value_should_return_true_and_key() {
        _sut.Add("a", 1);
        var result = _sut.TryGetKey(1, out var key);
        result.ShouldBeTrue();
        key.ShouldBe("a");
    }

    [Test]
    public void try_get_key_non_existing_value_should_return_false_and_default_key() {
        var result = _sut.TryGetKey(1, out var key);
        result.ShouldBeFalse();
        key.ShouldBe(default(string)); // null for string
    }

    [Test]
    public void clear_non_empty_dictionary_should_remove_all_items() {
        // Arrange
        _sut.Add("one", 1);
        _sut.Add("two", 2);

        // Act
        _sut.Clear();

        // Assert
        _sut.Count.ShouldBe(0);
        _sut.ContainsKey("one").ShouldBeFalse();
        _sut.ContainsValue(1).ShouldBeFalse();
        _sut.Keys.ShouldBeEmpty();
        _sut.Values.ShouldBeEmpty();
        _sut.ShouldBeEmpty(); // Checks GetEnumerator
    }

    [Test]
    public void count_should_reflect_number_of_items() {
        _sut.Count.ShouldBe(0);
        _sut.Add("a", 1);
        _sut.Count.ShouldBe(1);
        _sut.Add("b", 2);
        _sut.Count.ShouldBe(2);
        _sut.Remove("a");
        _sut.Count.ShouldBe(1);
        _sut.Clear();
        _sut.Count.ShouldBe(0);
    }

    [Test]
    public void keys_should_return_all_keys() {
        _sut.Add("one", 1);
        _sut.Add("two", 2);
        _sut.Add("three", 3);

        var keys = _sut.Keys;

        keys.ShouldBeUnique();
        keys.ShouldBe(["one", "two", "three"], true);
        keys.Count().ShouldBe(3); // Check count via enumeration
    }

    [Test]
    public void values_should_return_all_values() {
        _sut.Add("one", 1);
        _sut.Add("two", 2);
        _sut.Add("three", 3);

        var values = _sut.Values;

        values.ShouldBeUnique();
        values.ShouldBe([1, 2, 3], true);
        values.Count().ShouldBe(3); // Check count via enumeration
    }

    [Test]
    public void get_enumerator_should_iterate_over_all_pairs() {
        _sut.Add("one", 1);
        _sut.Add("two", 2);
        _sut.Add("three", 3);

        var pairs = new List<KeyValuePair<string, int>>();
        foreach (var kvp in _sut) pairs.Add(kvp);

        pairs.Count.ShouldBe(3);
        pairs.ShouldContain(kvp => kvp.Key == "one" && kvp.Value == 1);
        pairs.ShouldContain(kvp => kvp.Key == "two" && kvp.Value == 2);
        pairs.ShouldContain(kvp => kvp.Key == "three" && kvp.Value == 3);
    }

    [Test]
    public void custom_comparers_case_insensitive_should_work_correctly() {
        // Arrange
        using var sutCi = new ConcurrentBidirectionalDictionary<string, string>(
            StringComparer.OrdinalIgnoreCase,
            StringComparer.OrdinalIgnoreCase
        );

        // Act & Assert
        sutCi.Add("KeyOne", "ValueOne");
        sutCi.Count.ShouldBe(1);

        // ContainsKey (case-insensitive)
        sutCi.ContainsKey("keyone").ShouldBeTrue();
        sutCi.ContainsKey("KEYONE").ShouldBeTrue();
        sutCi.ContainsKey("KeyOne").ShouldBeTrue();
        sutCi.ContainsKey("KeyTwo").ShouldBeFalse();

        // ContainsValue (case-insensitive)
        sutCi.ContainsValue("valueone").ShouldBeTrue();
        sutCi.ContainsValue("VALUEONE").ShouldBeTrue();
        sutCi.ContainsValue("ValueOne").ShouldBeTrue();
        sutCi.ContainsValue("ValueTwo").ShouldBeFalse();

        // TryGetValue (case-insensitive key)
        sutCi.TryGetValue("KEYONE", out var val).ShouldBeTrue();
        val.ShouldBe("ValueOne");

        // TryGetKey (case-insensitive value)
        sutCi.TryGetKey("VALUEONE", out var key).ShouldBeTrue();
        key.ShouldBe("KeyOne");

        // Indexer Get (case-insensitive key)
        sutCi["keyone"].ShouldBe("ValueOne");

        // GetKeyByValue (case-insensitive value)
        sutCi.GetKeyByValue("valueone").ShouldBe("KeyOne");

        // Add duplicate key (case-insensitive) - should fail
        Should.Throw<ArgumentException>(() => sutCi.Add("keyone", "ValueTwo"));

        // Add duplicate value (case-insensitive) - should fail
        Should.Throw<ArgumentException>(() => sutCi.Add("KeyTwo", "valueone"));

        // Indexer set update (case-insensitive key)
        sutCi["KEYONE"] = "NewValue";
        sutCi.ContainsKey("KeyOne").ShouldBeTrue();
        sutCi.ContainsValue("ValueOne").ShouldBeFalse();
        sutCi.ContainsValue("NewValue").ShouldBeTrue();
        sutCi.GetKeyByValue("newvalue").ShouldBe("KEYONE"); // the value is still uppercase

        // Remove (case-insensitive key)
        sutCi.Remove("keyone").ShouldBeTrue();
        sutCi.Count.ShouldBe(0);
    }

    [Test]
    public void icollection_add_should_work() {
        ICollection<KeyValuePair<string, int>> coll = _sut;
        coll.Add(new KeyValuePair<string, int>("a", 1));

        _sut.Count.ShouldBe(1);
        _sut["a"].ShouldBe(1);
    }

    [Test]
    public void icollection_contains_existing_item_should_return_true() {
        _sut.Add("a", 1);
        ICollection<KeyValuePair<string, int>> coll = _sut;
        coll.Contains(new KeyValuePair<string, int>("a", 1)).ShouldBeTrue();
    }

    [Test]
    public void icollection_contains_non_existing_key_should_return_false() {
        _sut.Add("a", 1);
        ICollection<KeyValuePair<string, int>> coll = _sut;
        coll.Contains(new KeyValuePair<string, int>("b", 1)).ShouldBeFalse();
    }

    [Test]
    public void icollection_contains_key_exists_but_wrong_value_should_return_false() {
        _sut.Add("a", 1);
        ICollection<KeyValuePair<string, int>> coll = _sut;
        coll.Contains(new KeyValuePair<string, int>("a", 2)).ShouldBeFalse();
    }

    [Test]
    public void icollection_remove_existing_item_should_succeed() {
        _sut.Add("a", 1);
        _sut.Add("b", 2);
        ICollection<KeyValuePair<string, int>> coll = _sut;

        var result = coll.Remove(new KeyValuePair<string, int>("a", 1));

        result.ShouldBeTrue();
        _sut.Count.ShouldBe(1);
        _sut.ContainsKey("a").ShouldBeFalse();
        _sut.ContainsValue(1).ShouldBeFalse();
        _sut.ContainsKey("b").ShouldBeTrue();
    }

    [Test]
    public void icollection_remove_item_with_wrong_value_should_return_false() {
        _sut.Add("a", 1);
        _sut.Add("b", 2);
        ICollection<KeyValuePair<string, int>> coll = _sut;

        var result = coll.Remove(new KeyValuePair<string, int>("a", 99)); // Key exists, value doesn't match

        result.ShouldBeFalse();
        _sut.Count.ShouldBe(2); // Should not have removed anything
        _sut["a"].ShouldBe(1);
        _sut.GetKeyByValue(1).ShouldBe("a");
    }

    [Test]
    public void icollection_remove_non_existing_item_should_return_false() {
        _sut.Add("a", 1);
        ICollection<KeyValuePair<string, int>> coll = _sut;

        var result = coll.Remove(new KeyValuePair<string, int>("c", 3));

        result.ShouldBeFalse();
        _sut.Count.ShouldBe(1);
    }

    [Test]
    public void icollection_copy_to_should_copy_all_items() {
        _sut.Add("one", 1);
        _sut.Add("two", 2);
        _sut.Add("three", 3);
        ICollection<KeyValuePair<string, int>> coll = _sut;

        var array = new KeyValuePair<string, int>[5]; // Larger array
        coll.CopyTo(array, 1);                        // Copy starting at index 1

        array[0].ShouldBe(default); // Index 0 untouched
        array[1..4].ShouldBe([Kvp("one", 1), Kvp("two", 2), Kvp("three", 3)], true);
        array[4].ShouldBe(default); // Index 4 untouched
    }

    [Test]
    public void icollection_copy_to_throws_on_null_array() {
        ICollection<KeyValuePair<string, int>> coll = _sut;
        Should.Throw<NullReferenceException>(() => coll.CopyTo(null!, 0));
    }

    [Test]
    public void icollection_copy_to_throws_on_negative_index() {
        _sut.Add("a", 1);

        ICollection<KeyValuePair<string, int>> coll = _sut;

        var array = new KeyValuePair<string, int>[1];
        Should.Throw<IndexOutOfRangeException>(() => coll.CopyTo(array, -1));
    }

    [Test]
    public void icollection_copy_to_throws_on_insufficient_space() {
        _sut.Add("a", 1);
        _sut.Add("b", 2);

        ICollection<KeyValuePair<string, int>> coll = _sut;

        var array = new KeyValuePair<string, int>[3];

        Should.Throw<ArgumentException>(() => coll.CopyTo(array, 2)); // Only 1 space left, need 2
    }

    [Test]
    public void icollection_is_read_only_should_be_false() {
        ICollection<KeyValuePair<string, int>> coll = _sut;
        coll.IsReadOnly.ShouldBeFalse();
    }

    [Test]
    public async Task concurrent_adds_should_maintain_consistency() {
        // Arrange
        var numTasks     = 20;
        var itemsPerTask = 1000;
        var tasks        = new List<Task>();
        // Use the shared SUT instance (_sut)

        // Act
        for (var i = 0; i < numTasks; i++) {
            var taskNum = i;
            tasks.Add(
                Task.Run(() => {
                        for (var j = 0; j < itemsPerTask; j++) {
                            var itemNum = taskNum * itemsPerTask + j;
                            var key     = $"K{itemNum}";
                            var value   = itemNum;
                            _sut.TryAdd(key, value); // Use TryAdd for concurrency
                        }
                    }
                )
            );
        }

        await Task.WhenAll(tasks);

        // Assert
        _sut.Count.ShouldBe(numTasks * itemsPerTask);

        // Verify bidirectional consistency
        for (var i = 0; i < numTasks * itemsPerTask; i++) {
            var key   = $"K{i}";
            var value = i;
            _sut.ContainsKey(key).ShouldBeTrue($"Key {key} should exist");
            _sut.ContainsValue(value).ShouldBeTrue($"Value {value} should exist");
            _sut[key].ShouldBe(value, $"Key {key} should map to {value}");
            _sut.GetKeyByValue(value).ShouldBe(key, $"Value {value} should map to {key}");
        }
    }

    [Test]
    public async Task concurrent_add_remove_update_should_maintain_consistency() {
        // Arrange
        var numWriterTasks    = 10;
        var operationsPerTask = 500;
        var numReaderTasks    = 5;
        var initialItems      = 100;
        var tasks             = new List<Task>();
        var rnd               = new Random();
        // Use shared _sut

        // Pre-populate
        for (var i = 0; i < initialItems; i++) _sut.TryAdd($"Initial{i}", i);

        // Act
        // Writer tasks (Add, Remove, Update via indexer)
        for (var i = 0; i < numWriterTasks; i++) {
            var taskNum = i;
            tasks.Add(
                Task.Run(() => {
                        for (var j = 0; j < operationsPerTask; j++) {
                            var opType  = rnd.Next(3);                // 0: Add, 1: Remove, 2: Update
                            var itemNum = rnd.Next(initialItems * 2); // Target items in a wider range

                            try {
                                // TryAdd
                                if (opType == 0) {
                                    _sut.TryAdd($"Task{taskNum}-{j}", itemNum);
                                }
                                // TryRemove (Key or Value)
                                else if (opType == 1) {
                                    if (rnd.Next(2) == 0)
                                        _sut.Remove($"SomeKey{itemNum}"); // Try removing potential keys
                                    else
                                        _sut.RemoveByValue(itemNum); // Try removing potential values
                                }
                                // Update/Set
                                else {
                                    _sut[$"UpdateKey{itemNum}"] = itemNum + 10000; // Set potentially new or existing keys
                                }
                            }
                            catch (ArgumentException) {
                                /* Expected for some concurrent sets/adds */
                            }
                            catch (KeyNotFoundException) {
                                /* Expected for some concurrent removes */
                            }
                        }
                    }
                )
            );
        }

        // Reader tasks
        for (var i = 0; i < numReaderTasks; i++)
            tasks.Add(
                Task.Run(() => {
                        // More reads
                        for (var j = 0; j < operationsPerTask * 2; j++) {
                            var itemNum = rnd.Next(initialItems * 2);
                            _sut.TryGetValue($"SomeKey{itemNum}", out _);
                            _sut.TryGetKey(itemNum, out _);
                            _sut.ContainsKey($"AnotherKey{itemNum}");
                            _sut.ContainsValue(itemNum);
                            var currentCount = _sut.Count; // Access count
                            foreach (var _ in _sut) { }    // Try enumerating
                        }
                    }
                )
            );

        await Task.WhenAll(tasks);

        // Assert
        // The exact count is non-deterministic, but the internal state MUST be consistent.
        _sut.Count.ShouldBeGreaterThanOrEqualTo(0); // Sanity check count

        // *** The CRITICAL Assertion: Verify bidirectional consistency ***
        var forwardCopy = _sut.ToArray(); // Get a snapshot (uses GetEnumerator)

        forwardCopy.Length.ShouldBe(_sut.Count); // Snapshot count should match current count immediately after

        foreach (var (key, value) in forwardCopy) {
            // Check forward lookup consistency
            _sut.TryGetValue(key, out var retrievedValue).ShouldBeTrue($"Key '{key}' from snapshot must exist.");
            retrievedValue.ShouldBe(value, $"Key '{key}' must map to '{value}'.");

            // Check reverse lookup consistency
            _sut.TryGetKey(value, out var retrievedKey).ShouldBeTrue($"Value '{value}' from snapshot must exist.");
            retrievedKey.ShouldBe(key, $"Value '{value}' must map back to '{key}'.");
        }

        // Also verify the reverse dictionary side implicitly agrees on count
        // (Cannot access _reverse directly, but the loop above verifies all _reverse entries)
        // We can explicitly check all _reverse entries via the Values collection if desired:
        var values = _sut.Values.ToList(); // Snapshot values
        values.Count.ShouldBe(_sut.Count);
        foreach (var val in values) {
            _sut.TryGetKey(val, out var key).ShouldBeTrue();
            _sut.ContainsKey(key).ShouldBeTrue(); // Key must still exist
            _sut[key].ShouldBe(val);              // And map back to the value
        }
    }

    static KeyValuePair<TKey, TValue> Kvp<TKey, TValue>(TKey key, TValue value) => new(key, value);
}
