using System.Runtime.InteropServices;
using System.Text.Json;
using Grpc.Core;
using Kurrent.Client.Model;
using Kurrent.Variant;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a collection of error details specific to schema registry operations.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
	public readonly record struct SchemaNotFound(SchemaIdentifier Identifier) : IKurrentClientError {
		public string ErrorCode => nameof(SchemaNotFound);
		public string ErrorMessage => Identifier.IsSchemaName
			? $"Schema '{Identifier.AsSchemaName}' not found."
			: $"Schema version '{Identifier.AsSchemaVersionId}' not found.";
	}

	public readonly record struct SchemaAlreadyExists(SchemaName SchemaName) : IKurrentClientError {
		public string ErrorCode => nameof(SchemaAlreadyExists);
		public string ErrorMessage => $"Schema '{SchemaName}' already exists.";
	}

    public readonly record struct AccessDenied : IKurrentClientError {
        public string ErrorCode => nameof(AccessDenied);
        public string ErrorMessage => "Access denied to the requested resource.";
    }
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
	public static readonly Success Instance = new();
}

[PublicAPI]
public readonly partial record struct CreateSchemaError : IVariant<
	ErrorDetails.SchemaAlreadyExists,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct GetSchemaError : IVariant<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct GetSchemaVersionError : IVariant<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct DeleteSchemaError : IVariant<
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;

[PublicAPI]
public readonly partial record struct CheckSchemaCompatibilityError : IVariant<
	SchemaCompatibilityErrors,
	ErrorDetails.SchemaNotFound,
	ErrorDetails.AccessDenied>;
