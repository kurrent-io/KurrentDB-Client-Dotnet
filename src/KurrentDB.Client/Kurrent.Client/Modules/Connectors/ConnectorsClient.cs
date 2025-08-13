using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Kurrent.Client.Streams;
using KurrentDB.Protocol.Connectors.V2;
using static KurrentDB.Protocol.Connectors.V2.ConnectorsService;

namespace Kurrent.Client.Connectors;

/// <summary>
/// Client for managing connectors in KurrentDB.
/// </summary>
public class ConnectorsClient {
    internal ConnectorsClient(KurrentClient source) =>
        ServiceClient = new(source.LegacyCallInvoker);

    ConnectorsServiceClient ServiceClient { get; }

    /// <summary>
    /// Creates a new connector with the specified configuration.
    /// </summary>
    /// <param name="connectorId">Unique identifier for the connector.</param>
    /// <param name="name">Human-readable name for the connector.</param>
    /// <param name="settings">Configuration settings for the connector.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, CreateConnectorError>> CreateConnector(
        string connectorId,
        string? name = null,
        IReadOnlyDictionary<string, object>? settings = null,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new CreateConnector {
                ConnectorId = connectorId
            };

            if (name is not null)
                request.Name = name;

            if (settings is not null)
                request.Settings = ConvertToStruct(settings);

            await ServiceClient
                .CreateAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, CreateConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw KurrentException.CreateUnknown(nameof(CreateConnector), ex)
                }
            );
        }
    }

    /// <summary>
    /// Reconfigures an existing connector with new settings.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to reconfigure.</param>
    /// <param name="settings">Updated configuration settings for the connector.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, ReconfigureConnectorError>> ReconfigureConnector(
        string connectorId,
        IReadOnlyDictionary<string, object> settings,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ReconfigureConnector {
                ConnectorId = connectorId,
                Settings    = ConvertToStruct(settings)
            };

            await ServiceClient
                .ReconfigureAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, ReconfigureConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(ReconfigureConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(ReconfigureConnector), ex);
        }
    }

    /// <summary>
    /// Deletes an existing connector from the system.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, DeleteConnectorError>> DeleteConnector(
        string connectorId,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new DeleteConnector {
                ConnectorId = connectorId
            };

            await ServiceClient
                .DeleteAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, DeleteConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(DeleteConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(DeleteConnector), ex);
        }
    }

    /// <summary>
    /// Starts a connector, optionally from a specific position.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to start.</param>
    /// <param name="startPosition">Optional position to start processing from.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, StartConnectorError>> StartConnector(
        string connectorId,
        LogPosition startPosition,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new StartConnector {
                ConnectorId = connectorId
            };

            if (startPosition != LogPosition.Unset)
                request.StartPosition = startPosition;

            await ServiceClient
                .StartAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, StartConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(StartConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(StartConnector), ex);
        }
    }

    /// <summary>
    /// Resets a connector, optionally to a specific position.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to reset.</param>
    /// <param name="startPosition">Optional position to reset processing to.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, ResetConnectorError>> ResetConnector(
        string connectorId,
        long? startPosition = null,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ResetConnector {
                ConnectorId = connectorId
            };

            if (startPosition != LogPosition.Unset)
                request.StartPosition = startPosition;

            await ServiceClient
                .ResetAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, ResetConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(ResetConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(ResetConnector), ex);
        }
    }

    /// <summary>
    /// Stops a running connector.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to stop.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, StopConnectorError>> StopConnector(
        string connectorId,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new StopConnector {
                ConnectorId = connectorId
            };

            await ServiceClient
                .StopAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, StopConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(StopConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(StopConnector), ex);
        }
    }

    /// <summary>
    /// Renames an existing connector.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to rename.</param>
    /// <param name="newName">New display name for the connector.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result indicating success or specific error details.</returns>
    public async ValueTask<Result<Success, RenameConnectorError>> RenameConnector(
        string connectorId,
        string newName,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new RenameConnector {
                ConnectorId = connectorId,
                Name        = newName
            };

            await ServiceClient
                .RenameAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new Success();
        }
        catch (RpcException ex) {
            return Result.Failure<Success, RenameConnectorError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(RenameConnector), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(RenameConnector), ex);
        }
    }

    /// <summary>
    /// Lists connectors based on the specified filtering criteria.
    /// </summary>
    /// <param name="options">Options for filtering and pagination of connectors.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing the list of connectors or specific error details.</returns>
    public async ValueTask<Result<ConnectorListResult, ListConnectorsError>> ListConnectors(
        ConnectorListOptions? options = null,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new ListConnectorsRequest();

            if (options is not null) {
                foreach (var state in options.States) {
                    request.State.Add((KurrentDB.Protocol.Connectors.V2.ConnectorState)state);
                }

                foreach (var instanceType in options.InstanceTypeNames)
                    request.InstanceTypeName.Add(instanceType);

                foreach (var connectorId in options.ConnectorIds)
                    request.ConnectorId.Add(connectorId);

                request.IncludeSettings = options.IncludeSettings;
                request.ShowDeleted     = options.ShowDeleted;

                request.Paging = new Paging {
                    Page     = options.Page,
                    PageSize = options.PageSize
                };
            }

            var response = await ServiceClient.ListAsync(request, cancellationToken: cancellationToken);

            return response.ResultCase switch {
                ListConnectorsResponse.ResultOneofCase.Success => Result.Success<ConnectorListResult, ListConnectorsError>(
                    new ConnectorListResult {
                        Items     = response.Success.Items.Select(MapToConnectorDetails).ToList(),
                        TotalSize = response.Success.TotalSize
                    }
                ),
                ListConnectorsResponse.ResultOneofCase.Failure => Result.Failure<ConnectorListResult, ListConnectorsError>(
                    response.Failure.ErrorCase switch {
                        ListConnectorsFailure.ErrorOneofCase.AccessDenied => new ErrorDetails.AccessDenied(),
                        _ => throw new InvalidOperationException($"Unknown error case: {response.Failure.ErrorCase}")
                    }
                ),
                _ => throw new InvalidOperationException($"Unknown result case: {response.ResultCase}")
            };
        }
        catch (RpcException ex) {
            return Result.Failure<ConnectorListResult, ListConnectorsError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    _                           => throw KurrentException.CreateUnknown(nameof(ListConnectors), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(ListConnectors), ex);
        }
    }

    /// <summary>
    /// Retrieves the configuration settings for a specific connector.
    /// </summary>
    /// <param name="connectorId">Identifier of the connector to get settings for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Result containing the connector settings or specific error details.</returns>
    public async ValueTask<Result<ConnectorSettingsResult, GetConnectorSettingsError>> GetConnectorSettings(
        string connectorId,
        CancellationToken cancellationToken = default
    ) {
        try {
            var request = new GetConnectorSettingsRequest {
                ConnectorId = connectorId
            };

            var response = await ServiceClient.GetSettingsAsync(request, cancellationToken: cancellationToken);

            return response.ResultCase switch {
                GetConnectorSettingsResponse.ResultOneofCase.Success => Result.Success<ConnectorSettingsResult, GetConnectorSettingsError>(
                    new ConnectorSettingsResult {
                        Settings = new Dictionary<string, string>(),
                        // Settings = response.Success.Settings.ToDictionary(
                        //     kvp => kvp.Key,
                        //     kvp => kvp.Value?.Value ?? string.Empty
                        // ),
                        SettingsUpdateTime = response.Success.SettingsUpdateTime.ToDateTimeOffset()
                    }
                ),
                GetConnectorSettingsResponse.ResultOneofCase.Failure => Result.Failure<ConnectorSettingsResult, GetConnectorSettingsError>(
                    response.Failure.ErrorCase switch {
                        GetConnectorSettingsFailure.ErrorOneofCase.AccessDenied => new ErrorDetails.AccessDenied(),
                        GetConnectorSettingsFailure.ErrorOneofCase.ConnectorNotFound => new ErrorDetails.NotFound(x => x.With(
                                "connectorId", connectorId
                            )
                        ),
                        _ => throw new InvalidOperationException($"Unknown error case: {response.Failure.ErrorCase}")
                    }
                ),
                _ => throw new InvalidOperationException($"Unknown result case: {response.ResultCase}")
            };
        }
        catch (RpcException ex) {
            return Result.Failure<ConnectorSettingsResult, GetConnectorSettingsError>(
                ex.StatusCode switch {
                    StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                    StatusCode.NotFound         => new ErrorDetails.NotFound(),
                    _                           => throw KurrentException.CreateUnknown(nameof(GetConnectorSettings), ex)
                }
            );
        }
        catch (Exception ex) {
            throw KurrentException.CreateUnknown(nameof(GetConnectorSettings), ex);
        }
    }

    static ConnectorDetails MapToConnectorDetails(Connector connector) =>
        new() {
            ConnectorId      = connector.ConnectorId,
            InstanceTypeName = connector.InstanceTypeName,
            Name             = connector.Name,
            State            = (ConnectorState) connector.State,
            StateUpdateTime  = connector.StateUpdateTime.ToDateTimeOffset(),
            // Settings = connector.Settings.ToDictionary(
            //     kvp => kvp.Key,
            //     kvp => kvp.Value?.Value ?? string.Empty
            // ),
            SettingsUpdateTime = connector.SettingsUpdateTime.ToDateTimeOffset(),
            // Position           = connector.Position?.Value,
            PositionUpdateTime = connector.PositionUpdateTime?.ToDateTimeOffset(),
            CreateTime         = connector.CreateTime.ToDateTimeOffset(),
            UpdateTime         = connector.UpdateTime.ToDateTimeOffset(),
            DeleteTime         = connector.DeleteTime?.ToDateTimeOffset(),
            ErrorDetails       = connector.ErrorDetails?.Message
        };

    static Struct ConvertToStruct(IReadOnlyDictionary<string, object> dictionary) {
        var structValue = new Struct();
        foreach (var kvp in dictionary) {
            structValue.Fields[kvp.Key] = ConvertToValue(kvp.Value);
        }

        return structValue;


        static Value ConvertToValue(object obj) =>
            obj switch {
                null     => Value.ForNull(),
                string s => Value.ForString(s),
                bool b   => Value.ForBool(b),
                int i    => Value.ForNumber(i),
                double d => Value.ForNumber(d),
                float f  => Value.ForNumber(f),
                long l   => Value.ForNumber(l),
                _        => Value.ForString(obj.ToString() ?? string.Empty)
            };
    }

}
