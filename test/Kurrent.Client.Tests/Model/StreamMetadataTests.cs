// using System.Text.Json;
// using Kurrent.Client.Model;
//
// namespace Kurrent.Client.Tests.Model;
//
// public class StreamMetadataTests {
//     #region JSON Serialization Tests
//
//     [Test]
//     public void empty_metadata_serializes_to_empty_json() {
//         var metadata = new StreamMetadata();
//
//         var json = JsonSerializer.Serialize(metadata);
//
//         json.ShouldBe("{}");
//     }
//
//     [Test]
//     public void system_properties_serialize_with_correct_keys() {
//         var metadata = new StreamMetadata {
//             MaxAge         = TimeSpan.FromHours(24),
//             MaxCount       = 1000,
//             TruncateBefore = StreamRevision.From(42),
//             CacheControl   = TimeSpan.FromMinutes(30)
//         };
//
//         var json     = JsonSerializer.Serialize(metadata);
//         var document = JsonDocument.Parse(json);
//
//         document.RootElement.GetProperty("$maxAge").GetInt64().ShouldBe(86400); // 24 hours in seconds
//         document.RootElement.GetProperty("$maxCount").GetInt32().ShouldBe(1000);
//         document.RootElement.GetProperty("$tb").GetInt64().ShouldBe(42);
//         document.RootElement.GetProperty("$cacheControl").GetInt64().ShouldBe(1800); // 30 minutes in seconds
//     }
//
//     [Test]
//     public void acl_serializes_with_correct_structure() {
//         var metadata = new StreamMetadata {
//             Acl = new StreamAcl {
//                 ReadRoles      = ["admin", "user"],
//                 WriteRoles     = ["admin"],
//                 DeleteRoles    = ["admin"],
//                 MetaReadRoles  = ["admin"],
//                 MetaWriteRoles = ["admin"]
//             }
//         };
//
//         var json     = JsonSerializer.Serialize(metadata);
//         var document = JsonDocument.Parse(json);
//
//         var acl = document.RootElement.GetProperty("$acl");
//         acl.GetProperty("$r").EnumerateArray().Select(e => e.GetString()).ShouldBe(["admin", "user"]);
//         acl.GetProperty("$w").EnumerateArray().Select(e => e.GetString()).ShouldBe(["admin"]);
//         acl.GetProperty("$d").EnumerateArray().Select(e => e.GetString()).ShouldBe(["admin"]);
//         acl.GetProperty("$mr").EnumerateArray().Select(e => e.GetString()).ShouldBe(["admin"]);
//         acl.GetProperty("$mw").EnumerateArray().Select(e => e.GetString()).ShouldBe(["admin"]);
//     }
//
//     [Test]
//     public void custom_metadata_flattens_at_root_level() {
//         // Create metadata with system properties and custom properties
//         var metadata = new StreamMetadata {
//                 MaxAge = TimeSpan.FromSeconds(86400)
//             }
//             .WithCustomProperty("department", "sales")
//             .WithCustomProperty("priority", 5)
//             .WithCustomProperty("tags", new[] { "important", "customer-facing" })
//             .WithCustomProperty("enabled", true);
//
//         var json     = JsonSerializer.Serialize(metadata);
//         var document = JsonDocument.Parse(json);
//
//         // System property should be accessible as MaxAge
//         metadata.MaxAge.ShouldBe(TimeSpan.FromSeconds(86400));
//
//         // All properties should be at root level in JSON
//         document.RootElement.GetProperty("$maxAge").GetInt64().ShouldBe(86400);
//         document.RootElement.GetProperty("department").GetString().ShouldBe("sales");
//         document.RootElement.GetProperty("priority").GetInt32().ShouldBe(5);
//         document.RootElement.GetProperty("enabled").GetBoolean().ShouldBeTrue();
//     }
//
//     #endregion
//
//     #region JSON Deserialization Tests
//
//     [Test]
//     public void can_deserialize_empty_json() {
//         var json = "{}";
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.MaxAge.ShouldBeNull();
//         metadata.MaxCount.ShouldBeNull();
//         metadata.TruncateBefore.ShouldBeNull();
//         metadata.CacheControl.ShouldBeNull();
//         metadata.Acl.ShouldBeNull();
//     }
//
//     [Test]
//     public void can_deserialize_system_properties() {
//         var json = """{"$maxAge": 86400, "$maxCount": 1000, "$tb": 42, "$cacheControl": 1800}""";
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.MaxAge.ShouldBe(TimeSpan.FromHours(24));
//         metadata.MaxCount.ShouldBe(1000);
//         metadata.TruncateBefore.ShouldBe(StreamRevision.From(42));
//         metadata.CacheControl.ShouldBe(TimeSpan.FromMinutes(30));
//     }
//
//     [Test]
//     public void can_deserialize_acl() {
//         var json = """{"$acl": {"$r": ["admin", "user"], "$w": ["admin"]}}""";
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.Acl.ShouldNotBeNull();
//         metadata.Acl.Value.ReadRoles.ShouldBe(["admin", "user"]);
//         metadata.Acl.Value.WriteRoles.ShouldBe(["admin"]);
//         metadata.Acl.Value.DeleteRoles.ShouldBe([]);
//     }
//
//     [Test]
//     public void can_deserialize_mixed_system_and_custom_properties() {
//         var json = """{"$maxAge": 3600, "department": "sales", "priority": 5, "enabled": true}""";
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.MaxAge.ShouldBe(TimeSpan.FromHours(1));
//         metadata.GetCustomProperty<string>("department").ShouldBe("sales");
//         metadata.GetCustomProperty<int>("priority").ShouldBe(5);
//         metadata.GetCustomProperty<bool>("enabled").ShouldBe(true);
//     }
//
//     #endregion
//
//     #region Property Access Tests
//
//     [Test]
//     public void properties_can_be_set_via_object_initialization() {
//         var metadata = new StreamMetadata {
//             MaxAge         = TimeSpan.FromDays(7),
//             MaxCount       = 5000,
//             TruncateBefore = StreamRevision.From(100),
//             CacheControl   = TimeSpan.FromHours(2),
//             Acl            = new StreamAcl { ReadRoles = ["public"] }
//         };
//
//         metadata.MaxAge.ShouldBe(TimeSpan.FromDays(7));
//         metadata.MaxCount.ShouldBe(5000);
//         metadata.TruncateBefore.ShouldBe(StreamRevision.From(100));
//         metadata.CacheControl.ShouldBe(TimeSpan.FromHours(2));
//         metadata.Acl.ShouldNotBeNull();
//         metadata.Acl.Value.ReadRoles.ShouldBe(["public"]);
//     }
//
//     [Test]
//     public void has_properties_return_correct_values() {
//         var metadata = new StreamMetadata {
//             MaxAge         = TimeSpan.FromMinutes(1),
//             MaxCount       = 10,
//             TruncateBefore = StreamRevision.From(5),
//             CacheControl   = TimeSpan.FromSeconds(30),
//             Acl            = new StreamAcl { ReadRoles = ["test"] }
//         };
//
//         metadata.HasMaxAge.ShouldBeTrue();
//         metadata.HasMaxCount.ShouldBeTrue();
//         metadata.HasTruncateBefore.ShouldBeTrue();
//         metadata.HasCacheControl.ShouldBeTrue();
//         metadata.HasAcl.ShouldBeTrue();
//     }
//
//     [Test]
//     public void has_properties_return_false_for_default_values() {
//         var metadata = new StreamMetadata {
//             MaxAge       = TimeSpan.Zero,
//             MaxCount     = 0,
//             CacheControl = TimeSpan.Zero
//         };
//
//         metadata.HasMaxAge.ShouldBeFalse();
//         metadata.HasMaxCount.ShouldBeFalse();
//         metadata.HasCacheControl.ShouldBeFalse();
//         metadata.HasTruncateBefore.ShouldBeFalse();
//         metadata.HasAcl.ShouldBeFalse();
//     }
//
//     #endregion
//
//     #region Custom Metadata Tests
//
//     [Test]
//     public void custom_metadata_excludes_system_properties() {
//         var properties = new Dictionary<string, object?> {
//             ["$maxAge"]    = 3600L,
//             ["$acl"]       = new { },
//             ["department"] = "engineering",
//             ["version"]    = "1.2.3",
//             ["$maxCount"]  = 1000
//         };
//
//         var metadata = new StreamMetadata();
//         // Set properties using WithCustomProperties since Properties is now internal
//         metadata = metadata.WithCustomProperties(properties.Where(kvp => !kvp.Key.StartsWith("$")).ToDictionary());
//
//         var customMetadata = metadata.CustomProperties;
//
//         customMetadata.Count.ShouldBe(2);
//         metadata.GetCustomProperty<string>("department").ShouldBe("engineering");
//         metadata.GetCustomProperty<string>("version").ShouldBe("1.2.3");
//         customMetadata.ShouldNotContainKey("$maxAge");
//         customMetadata.ShouldNotContainKey("$acl");
//         customMetadata.ShouldNotContainKey("$maxCount");
//     }
//
//     [Test]
//     public void custom_metadata_handles_complex_objects() {
//         var complexObject = new {
//             nested    = new { value = 42 },
//             array     = new[] { 1, 2, 3 },
//             nullValue = (string?)null
//         };
//
//         var properties = new Dictionary<string, object?> {
//             ["complex"] = complexObject,
//             ["simple"]  = "test"
//         };
//
//         var metadata = new StreamMetadata().WithCustomProperties(properties);
//
//         var customMetadata = metadata.CustomProperties;
//         customMetadata.ShouldContainKey("complex");
//         customMetadata.ShouldContainKey("simple");
//         customMetadata["simple"].ShouldBe("test");
//     }
//
//     [Test]
//     public void has_custom_metadata_returns_correct_value() {
//         var emptyMetadata = new StreamMetadata();
//         emptyMetadata.HasCustomMetadata.ShouldBeFalse();
//
//         var systemOnlyMetadata = new StreamMetadata { MaxAge = TimeSpan.FromHours(1) };
//         systemOnlyMetadata.HasCustomMetadata.ShouldBeFalse();
//
//         var customMetadata = new StreamMetadata { MaxAge = TimeSpan.FromHours(1) }
//             .WithCustomProperty("custom", "value");
//
//         customMetadata.HasCustomMetadata.ShouldBeTrue();
//     }
//
//     #endregion
//
//     #region Edge Cases
//
//     [Test]
//     public void handles_null_acl_gracefully() {
//         var json = """{"$acl": null}""";
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.Acl.ShouldBeNull();
//         metadata.HasAcl.ShouldBeFalse();
//     }
//
//     [Test]
//     public void handles_stream_revision_max_value() {
//         var json = """{"$tb": 9223372036854775807}"""; // long.MaxValue
//
//         var metadata = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         metadata.ShouldNotBeNull();
//         metadata.TruncateBefore.ShouldBe(StreamRevision.Max);
//     }
//
//     [Test]
//     public void round_trip_serialization_preserves_data() {
//         var original = new StreamMetadata {
//             MaxAge         = TimeSpan.FromHours(48),
//             MaxCount       = 2500,
//             TruncateBefore = StreamRevision.From(123),
//             CacheControl   = TimeSpan.FromMinutes(45),
//             Acl = new StreamAcl {
//                 ReadRoles      = ["reader1", "reader2"],
//                 WriteRoles     = ["writer"],
//                 DeleteRoles    = ["admin"],
//                 MetaReadRoles  = ["meta-reader"],
//                 MetaWriteRoles = ["meta-writer"]
//             }
//         }.WithCustomProperties(
//             new Dictionary<string, object?> {
//                 ["custom1"] = "value1",
//                 ["custom2"] = 42,
//                 ["custom3"] = true
//             }
//         );
//
//         var json         = JsonSerializer.Serialize(original);
//         var deserialized = JsonSerializer.Deserialize<StreamMetadata>(json);
//
//         deserialized.ShouldNotBeNull();
//         deserialized.MaxAge.ShouldBe(original.MaxAge);
//         deserialized.MaxCount.ShouldBe(original.MaxCount);
//         deserialized.TruncateBefore.ShouldBe(original.TruncateBefore);
//         deserialized.CacheControl.ShouldBe(original.CacheControl);
//         deserialized.Acl.ShouldBe(original.Acl);
//
//         var customMetadata = deserialized.CustomProperties;
//         deserialized.GetCustomProperty<string>("custom1").ShouldBe("value1");
//         deserialized.GetCustomProperty<int>("custom2").ShouldBe(42);
//         deserialized.GetCustomProperty<bool>("custom3").ShouldBe(true);
//     }
//
//     #endregion
// }
