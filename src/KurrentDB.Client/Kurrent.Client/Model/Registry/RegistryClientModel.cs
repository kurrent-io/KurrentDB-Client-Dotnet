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

[PublicAPI]
public partial class CreateSchemaError : IWhatever<ErrorDetails.SchemaAlreadyExists>;

[PublicAPI]
public partial class GetSchemaError : IWhatever<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public partial class GetSchemaVersionError : IWhatever<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public partial class DeleteSchemaError : IWhatever<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public partial class CheckSchemaCompatibilityError : IWhatever<SchemaCompatibilityErrors, ErrorDetails.SchemaNotFound>;
