using System.Runtime.InteropServices;
using Kurrent.Client.Model;
using Kurrent.Variant;
using static KurrentDB.Protocol.Registry.V2.ErrorDetails;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a collection of error details specific to schema registry operations.
/// </summary>
[PublicAPI]
public static partial class ErrorDetails {
    /// <summary>
    /// Represents an error indicating that the specified stream could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.SchemaNotFound))]
    public readonly partial record struct SchemaNotFound;

    /// <summary>
    /// Represents an error indicating that access to the requested resource has been denied.
    /// </summary>
    [KurrentOperationError(typeof(Types.AccessDenied))]
    public readonly partial record struct AccessDenied;

    /// <summary>
    /// Represents an error indicating that the specified schema version could not be found.
    /// </summary>
    [KurrentOperationError(typeof(Types.SchemaAlreadyExists))]
    public readonly partial record struct SchemaAlreadyExists;
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
	public static readonly Success Instance = new();
}

[PublicAPI]
public readonly partial record struct CreateSchemaError : IVariantResultError<
	ErrorDetails.SchemaAlreadyExists,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct GetSchemaError : IVariantResultError<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct GetSchemaVersionError : IVariantResultError<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct DeleteSchemaError : IVariantResultError<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct CheckSchemaCompatibilityError : IVariantResultError<
	SchemaCompatibilityErrors,
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;
