using System.Runtime.InteropServices;
using Kurrent.Client.Model;
using Kurrent.Whatever;
using OneOf;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a collection of error details specific to schema registry operations.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
    /// <summary>
    ///  Represents an error indicating that the specified schema or schema version was not found.
    /// </summary>
    public record SchemaNotFound : KurrentClientErrorDetails {
        /// <summary>
        /// Represents an error indicating that the specified schema or schema version was not found.
        /// </summary>
        /// <param name="identifier">
        /// The identifier of the schema that could not be located.
        /// </param>
        public SchemaNotFound(SchemaIdentifier identifier) : base() {
            SchemaIdentifier = identifier;

            ErrorMessage = SchemaIdentifier.IsSchemaName
                ? $"Schema '{SchemaIdentifier.AsSchemaName}' not found."
                : $"Schema version '{SchemaIdentifier.AsSchemaVersionId}' not found.";
        }

        SchemaIdentifier SchemaIdentifier { get; }

        public override string ErrorMessage { get; }
    }

    /// <summary>
    /// Represents an error indicating that the specified stream already exists.
    /// </summary>
    /// <param name="Stream">The name of the stream.</param>
    public record SchemaAlreadyExists(SchemaName Stream) : KurrentClientErrorDetails {
        public override string ErrorMessage => $"Stream '{Stream}' already exists.";
    }
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
    public static readonly Success Instance = new();
}

// TODO: all results should be represented using Result<T, E>
// TODO: all Errors should be represented using OneOf<ErrorDetails.Something, ErrorDetails.SomethingElse>

[GenerateOneOf]
public partial class CreateSchemaResult : OneOfBase<SchemaVersionDescriptor, ErrorDetails.SchemaAlreadyExists> {
    public bool IsSchemaVersionDescriptor => IsT0;
    public bool IsSchemaAlreadyExists     => IsT1;

    public SchemaVersionDescriptor          AsSchemaVersionDescriptor => AsT0;
    public ErrorDetails.SchemaAlreadyExists AsSchemaAlreadyExists     => AsT1;
}

[GenerateOneOf]
public partial class GetSchemaResult : OneOfBase<Schema, ErrorDetails.SchemaNotFound> {
    public bool IsSchema         => IsT0;
    public bool IsSchemaNotFound => IsT1;

    public Schema                      AsSchema         => AsT0;
    public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class GetSchemaVersionResult : OneOfBase<SchemaVersion, ErrorDetails.SchemaNotFound> {
    public bool IsSchemaVersion  => IsT0;
    public bool IsSchemaNotFound => IsT1;

    public SchemaVersion               AsSchemaVersion  => AsT0;
    public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class DeleteSchemaResult : OneOfBase<Success, ErrorDetails.SchemaNotFound> {
    public bool IsSuccess        => IsT0;
    public bool IsSchemaNotFound => IsT1;

    public Success                     AsSuccess        => AsT0;
    public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class CheckSchemaCompatibilityResult : OneOfBase<SchemaVersionId, SchemaCompatibilityErrors, ErrorDetails.SchemaNotFound> {
    public bool IsSchemaVersionId => IsT0;
    public bool IsSchemaErrors    => IsT1;
    public bool IsSchemaNotFound  => IsT2;

    public SchemaVersionId             AsSchemaVersionId => AsT0;
    public SchemaCompatibilityErrors   AsSchemaErrors    => AsT1;
    public ErrorDetails.SchemaNotFound AsSchemaNotFound  => AsT2;
}
