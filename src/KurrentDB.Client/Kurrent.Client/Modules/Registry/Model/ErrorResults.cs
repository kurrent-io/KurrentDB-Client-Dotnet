using Kurrent.Variant;

namespace Kurrent.Client.Registry;

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
