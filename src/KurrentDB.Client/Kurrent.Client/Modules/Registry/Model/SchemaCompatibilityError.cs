using System.Collections;
using Google.Protobuf.Collections;
using NJsonSchema;

using Contracts = KurrentDB.Protocol.Registry.V2;

namespace Kurrent.Client.Registry;

public readonly record struct SchemaCompatibilityError() {
    public SchemaCompatibilityErrorKind Kind    { get; init; } = SchemaCompatibilityErrorKind.Unspecified;
    public string                       Details { get; init; } = string.Empty;

    public string         PropertyPath { get; init; } = string.Empty;
    public JsonObjectType OriginalType { get; init; } = JsonObjectType.None;
    public JsonObjectType NewType      { get; init; } = JsonObjectType.None;

    public override string ToString() =>
        $"{Kind} at '{PropertyPath}': {Details}";

    internal static SchemaCompatibilityError FromProto(KurrentDB.Protocol.Registry.V2.SchemaCompatibilityError error) {
        if (error is null) throw new ArgumentNullException(nameof(error));

        return new SchemaCompatibilityError {
            Kind         = (SchemaCompatibilityErrorKind)error.Kind,
            Details      = error.Details,
            PropertyPath = error.PropertyPath,
            OriginalType = Enum.TryParse<JsonObjectType>(error.OriginalType, out var originalType) ? originalType : JsonObjectType.None,
            NewType      = Enum.TryParse<JsonObjectType>(error.NewType, out var newType) ? newType : JsonObjectType.None
        };
    }
}

public readonly record struct SchemaCompatibilityErrors() : IResultError, IEnumerable<SchemaCompatibilityError> {
    public IReadOnlyList<SchemaCompatibilityError> Errors { get; init; } = [];

    public static SchemaCompatibilityErrors From(params SchemaCompatibilityError[] errors) =>
        new() { Errors = errors };

    public static implicit operator SchemaCompatibilityErrors(SchemaCompatibilityError[] errors) =>
        new() { Errors = errors };

    internal static SchemaCompatibilityErrors FromProto(RepeatedField<Contracts.SchemaCompatibilityError> errors) =>
        new() { Errors = errors.Select(SchemaCompatibilityError.FromProto).ToArray() };

    public IEnumerator<SchemaCompatibilityError> GetEnumerator() => Errors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public string ErrorCode => nameof(SchemaCompatibilityErrors);

    public string ErrorMessage =>
        $"Schema compatibility check failed with {Errors.Count} error(s): {string.Join("; ", Errors.Select(e => e.ToString()))}";

    public Exception CreateException(Exception? innerException = null) =>
        new InvalidOperationException(ErrorMessage, innerException);
}

public enum SchemaCompatibilityErrorKind {
    Unspecified,
    MissingRequiredProperty,  // Backward compatibility: Required property from old schema missing in new schema
    IncompatibleTypeChange,   // Backward compatibility: Property type changed incompatibly
    OptionalToRequired,       // Backward compatibility: Property changed from optional to required
    NewRequiredProperty,      // Forward compatibility: New required property added
    RemovedProperty,          // Forward compatibility: Property removed from schema
    ArrayTypeIncompatibility, // Issues with array item types
    DataFormatMismatch,       // Data format mismatch between schema versions
}
