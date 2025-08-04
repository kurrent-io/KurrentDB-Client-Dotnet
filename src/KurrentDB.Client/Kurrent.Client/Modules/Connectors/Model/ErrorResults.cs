using Kurrent.Variant;

namespace Kurrent.Client.Connectors;

[PublicAPI]
public readonly partial record struct CreateConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct ReconfigureConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct DeleteConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct StartConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct ResetConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct StopConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct RenameConnectorError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;

[PublicAPI]
public readonly partial record struct ListConnectorsError : IVariantResultError<
    ErrorDetails.AccessDenied
>;

[PublicAPI]
public readonly partial record struct GetConnectorSettingsError : IVariantResultError<
    ErrorDetails.AccessDenied,
    ErrorDetails.ConnectorNotFound
>;
