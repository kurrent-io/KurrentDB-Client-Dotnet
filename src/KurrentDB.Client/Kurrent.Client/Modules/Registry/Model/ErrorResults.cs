using Kurrent.Variant;

namespace Kurrent.Client.Registry;

[PublicAPI]
public readonly partial record struct CreateSchemaError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.AlreadyExists,
    ErrorDetails.FailedPrecondition>;

[PublicAPI]
public readonly partial record struct GetSchemaError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct GetSchemaVersionError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct DeleteSchemaError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound>;

[PublicAPI]
public readonly partial record struct CheckSchemaCompatibilityError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.NotFound,
    SchemaCompatibilityErrors>;
