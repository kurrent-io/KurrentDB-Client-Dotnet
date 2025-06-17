using System.Runtime.InteropServices;
using Kurrent.Client.Model;
using Kurrent.Variant;
using OneOf;

namespace Kurrent.Client.SchemaRegistry;

/// <summary>
/// Represents a collection of error details specific to schema registry operations.
/// </summary>
[PublicAPI]
public static class ErrorDetails {
	/// <summary>
	/// Represents an error indicating that the specified schema or schema version was not found.
	/// </summary>
	/// <param name="Identifier">The identifier of the schema that could not be located.</param>
	public readonly record struct SchemaNotFound(SchemaIdentifier Identifier) : IKurrentClientError {
		/// <summary>
		/// Gets the unique code identifying the error.
		/// </summary>
		public string ErrorCode => nameof(SchemaNotFound);

		/// <summary>
		/// Gets the error message indicating that the schema or schema version was not found.
		/// </summary>
		public string ErrorMessage => Identifier.IsSchemaName
			? $"Schema '{Identifier.AsSchemaName}' not found."
			: $"Schema version '{Identifier.AsSchemaVersionId}' not found.";
	}

	/// <summary>
	/// Represents an error indicating that the specified schema already exists.
	/// </summary>
	/// <param name="SchemaName">The name of the schema.</param>
	public readonly record struct SchemaAlreadyExists(SchemaName SchemaName) : IKurrentClientError {
		/// <summary>
		/// Gets the unique code identifying the error.
		/// </summary>
		public string ErrorCode => nameof(SchemaAlreadyExists);

		/// <summary>
		/// Gets the error message indicating that the schema already exists.
		/// </summary>
		public string ErrorMessage => $"Schema '{SchemaName}' already exists.";
	}
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
	public static readonly Success Instance = new();
}

[PublicAPI]
public readonly partial record struct CreateSchemaError : IVariant<ErrorDetails.SchemaAlreadyExists>;

[PublicAPI]
public readonly partial record struct GetSchemaError : IVariant<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public readonly partial record struct GetSchemaVersionError : IVariant<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public readonly partial record struct DeleteSchemaError : IVariant<ErrorDetails.SchemaNotFound>;

[PublicAPI]
public readonly partial record struct CheckSchemaCompatibilityError : IVariant<SchemaCompatibilityErrors, ErrorDetails.SchemaNotFound>;
