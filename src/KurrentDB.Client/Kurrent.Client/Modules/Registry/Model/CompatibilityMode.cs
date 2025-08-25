using System.ComponentModel;

namespace Kurrent.Client.Registry;

public enum CompatibilityMode {
    /// <summary>
    /// Default value, should not be used.
    /// </summary>
    [Description("COMPATIBILITY_MODE_UNSPECIFIED")] Unspecified = 0,
    /// <summary>
    /// Backward compatibility allows new schemas to be used with data written by previous schemas.
    /// Example: If schema version 1 has a field "name" and schema version 2 adds a new field "age",
    /// data written with schema version 1 can still be read using schema version 2.
    /// Example of invalid schema: If schema version 1 has a field "name" and schema version 2 removes the "name" field,
    /// data written with schema version 1 cannot be read using schema version 2.
    /// </summary>
    [Description("COMPATIBILITY_MODE_BACKWARD")] Backward = 1,
    /// <summary>
    /// Forward compatibility allows data written by new schemas to be read by previous schemas.
    /// Example: If schema version 1 has a field "name" and schema version 2 adds a new field "age",
    /// data written with schema version 2 can still be read using schema version 1, ignoring the "age" field.
    /// Example of invalid schema: If schema version 1 has a field "name" and schema version 2 changes the "name" field type,
    /// data written with schema version 2 cannot be read using schema version 1.
    /// </summary>
    [Description("COMPATIBILITY_MODE_FORWARD")] Forward = 2,
    /// <summary>
    /// Full compatibility ensures both backward and forward compatibility.
    /// This mode guarantees that new schemas can read data written by old schemas,
    /// and old schemas can read data written by new schemas.
    /// </summary>
    [Description("COMPATIBILITY_MODE_FULL")] Full = 3,
    /// <summary>
    /// Disables compatibility checks, allowing any kind of schema change.
    /// This mode should be used with caution, as it may lead to compatibility issues.
    /// </summary>
    [Description("COMPATIBILITY_MODE_NONE")] None = 4,
}
