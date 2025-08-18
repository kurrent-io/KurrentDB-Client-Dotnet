// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Google.Protobuf;
using Grpc.Core;
using Kurrent.Client.Streams;
using static KurrentDB.Protocol.Registry.V2.SchemaRegistryService;
using Contracts = KurrentDB.Protocol.Registry.V2;

namespace Kurrent.Client.Registry;

public class RegistryClient {
	internal RegistryClient(KurrentClient source) =>
		ServiceClient = new(source.LegacyCallInvoker);

	SchemaRegistryServiceClient ServiceClient { get; }

	/// <summary>
	/// Creates a new schema in the registry with the provided details.
	/// </summary>
	/// <param name="schemaName">
	/// The unique name of the schema to create.
	/// </param>
	/// <param name="schemaDefinition">
	/// The schema definition in string format, representing the structure of the schema.
	/// </param>
	/// <param name="dataFormat">
	/// The format of the schema, such as Json, Protobuf, Avro, or Bytes.
	/// </param>
	/// <param name="compatibilityMode">
	/// Specifies the compatibility mode for the schema, which defines whether and how new versions
	/// of the schema need to maintain compatibility with previous versions.
	/// </param>
	/// <param name="description">
	/// A human-readable description of the schema being created.
	/// </param>
	/// <param name="tags">
	/// A dictionary of key-value pairs that provide additional metadata or categorization for the schema.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{SchemaVersionDescriptor, CreateSchemaError}"/> representing the outcome of the schema creation,
	/// which may contain the newly generated schema version or an error in case of failure.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public async ValueTask<Result<SchemaVersionDescriptor, CreateSchemaError>> CreateSchema(
		SchemaName schemaName, string schemaDefinition,
		SchemaDataFormat dataFormat, CompatibilityMode compatibilityMode,
		string description, Dictionary<string, string> tags,
		CancellationToken cancellationToken = default
	) {
		var request = new Contracts.CreateSchemaRequest {
			SchemaName = schemaName,
			Details    = new() {
				DataFormat    = (Contracts.SchemaDataFormat)dataFormat,
				Compatibility = (Contracts.CompatibilityMode)compatibilityMode,
				Description   = description,
				Tags          = { tags }
			},
			SchemaDefinition = ByteString.CopyFromUtf8(schemaDefinition)
		};

        try {
            var response = await ServiceClient
                .CreateSchemaAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return new SchemaVersionDescriptor(Guid.Parse(response.SchemaVersionId), response.VersionNumber);
        }
        catch (RpcException rex) {
            return Result.Failure<SchemaVersionDescriptor, CreateSchemaError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.AlreadyExists    => new ErrorDetails.AlreadyExists(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	/// <summary>
	/// Retrieves the schema details for the specified schema name from the registry.
	/// </summary>
	/// <param name="schemaName">
	/// The unique name of the schema to retrieve.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{Schema, GetSchemaError}"/> that either contains the retrieved schema or an error indicating the issue encountered, such as the schema not being found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if an error occurs while trying to retrieve the schema, including issues with network connectivity or invalid input.
	/// </exception>
	public async ValueTask<Result<Schema, GetSchemaError>> GetSchema(SchemaName schemaName, CancellationToken cancellationToken = default) {
		var request = new Contracts.GetSchemaRequest {
			SchemaName = schemaName
		};

        try {
            var response = await ServiceClient
                .GetSchemaAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Schema.FromProto(response.Schema);
        }
        catch (RpcException rex) {
            return Result.Failure<Schema, GetSchemaError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	/// <summary>
	/// Retrieves the specific version of a schema from the registry based on the provided details.
	/// </summary>
	/// <param name="schemaName">
	/// The unique identifier of the schema whose version is being retrieved.
	/// </param>
	/// <param name="versionNumber">
	/// The version number of the schema to retrieve. If not provided, the latest version will be fetched.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{SchemaVersion, GetSchemaVersionError}"/> containing either the schema version details or an error indicating that the schema could not be found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if an error occurs during the operation, such as network issues or invalid input.
	/// </exception>
	public async ValueTask<Result<SchemaVersion, GetSchemaVersionError>> GetSchemaVersion(
		SchemaName schemaName, int? versionNumber = null, CancellationToken cancellationToken = default
	) {
		var request = new Contracts.GetSchemaVersionRequest { SchemaName = schemaName };

		if (versionNumber.HasValue)
			request.VersionNumber = versionNumber.Value;

        try {
            var response = await ServiceClient
                .GetSchemaVersionAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return SchemaVersion.FromProto(response.Version);
        }
        catch (RpcException rex) {
            return Result.Failure<SchemaVersion, GetSchemaVersionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	/// <summary>
	/// Retrieves the schema version details for the specified schema version ID from the registry.
	/// </summary>
	/// <param name="schemaVersionId">
	/// A <see cref="SchemaVersionId"/> that represents the unique identifier of the schema version to retrieve.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{SchemaVersion, GetSchemaVersionError}"/> containing the requested schema version details or an error
	/// if the schema version is not found.
	/// </returns>
	/// <exception cref="RpcException">
	/// Throws an exception in case of communication errors with the schema registry service.
	/// </exception>
	public async ValueTask<Result<SchemaVersion, GetSchemaVersionError>> GetSchemaVersionById(
		SchemaVersionId schemaVersionId, CancellationToken cancellationToken = default
	) {
		var request = new Contracts.GetSchemaVersionByIdRequest { SchemaVersionId = schemaVersionId };

        try {
            var response = await ServiceClient
                .GetSchemaVersionByIdAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return SchemaVersion.FromProto(response.Version);
        }
        catch (RpcException rex) {
            return Result.Failure<SchemaVersion, GetSchemaVersionError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

	/// <summary>
	/// Deletes an existing schema in the registry identified by the specified name, and all its versions.
	/// </summary>
	/// <param name="schemaName">
	/// The unique name of the schema to delete.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{Success, DeleteSchemaError}"/> indicating the result of the schema deletion operation.
	/// The result can either indicate success or specify that the schema was not found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public async ValueTask<Result<Success, DeleteSchemaError>> DeleteSchema(SchemaName schemaName, CancellationToken cancellationToken = default) {
		var request = new Contracts.DeleteSchemaRequest { SchemaName = schemaName };

        try {
            _ = await ServiceClient
                .DeleteSchemaAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return Success.Instance;
        }
        catch (RpcException rex) {
            return Result.Failure<Success, DeleteSchemaError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}

    /// <summary>
	/// Checks the compatibility of a schema against a given schema identifier and data format.
	/// </summary>
	/// <param name="identifier">
	/// The schema identifier, which can either be a schema name or a schema version ID.
	/// </param>
	/// <param name="schemaDefinition">
	/// The schema definition to check for compatibility in string format.
	/// </param>
	/// <param name="dataFormat">
	/// The format of the schema, such as Json, Protobuf, Avro, or Bytes.
	/// </param>
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="Result{TSuccess, TError}"/> of type <see cref="SchemaVersionId"/> on success, or
	/// <see cref="CheckSchemaCompatibilityError"/> on failure indicating details of the compatibility issues.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if there is an error during the compatibility check process.
	/// </exception>
	public async ValueTask<Result<SchemaVersionId, CheckSchemaCompatibilityError>> CheckSchemaCompatibility(
		SchemaIdentifier identifier, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default
	) {
        var request = new Contracts.CheckSchemaCompatibilityRequest {
            Definition = ByteString.CopyFromUtf8(schemaDefinition),
            DataFormat = (Contracts.SchemaDataFormat)dataFormat
        };

        identifier.Switch(
            sn => request.SchemaName = sn,
            sv => request.SchemaVersionId = sv
        );

        try {
            var response = await ServiceClient
                .CheckSchemaCompatibilityAsync(request, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return response.Success is not null
                ? Result.Success<SchemaVersionId, CheckSchemaCompatibilityError>(SchemaVersionId.From(response.Success.SchemaVersionId))
                : Result.Failure<SchemaVersionId, CheckSchemaCompatibilityError>(SchemaCompatibilityErrors.FromProto(response.Failure.Errors));
        }
        catch (RpcException rex) {
            return Result.Failure<SchemaVersionId, CheckSchemaCompatibilityError>(rex.StatusCode switch {
                StatusCode.PermissionDenied => new ErrorDetails.AccessDenied(),
                StatusCode.NotFound         => new ErrorDetails.NotFound(m => m.With(identifier.IsSchemaName ? "SchemaName" : "SchemaVersionId", identifier)),
                _                           => throw rex.WithOriginalCallStack()
            });
        }
	}
}
