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
			Details = new Contracts.SchemaDetails {
				DataFormat    = (Contracts.SchemaDataFormat)dataFormat,
				Compatibility = (Contracts.CompatibilityMode)compatibilityMode,
				Description   = description,
				Tags          = { tags }
			},
			SchemaDefinition = ByteString.CopyFromUtf8(schemaDefinition)
		};

		return await ServiceClient
			.CreateSchemaAsync(request, cancellationToken: cancellationToken)
			.ResponseAsync
			.ToResultAsync()
			.MatchAsync(
				onSuccess: response =>
					Result.Success<SchemaVersionDescriptor, CreateSchemaError>(
						new SchemaVersionDescriptor(Guid.Parse(response.SchemaVersionId), response.VersionNumber)
					),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.AlreadyExists    => Result.Failure<SchemaVersionDescriptor, CreateSchemaError>(new ErrorDetails.SchemaAlreadyExists(m => m.With("stream", schemaName))),
							StatusCode.PermissionDenied => Result.Failure<SchemaVersionDescriptor, CreateSchemaError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(CreateSchema), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
	}

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
	/// <param name="cancellationToken">
	/// An optional <see cref="CancellationToken"/> to observe while waiting for the operation to complete.
	/// </param>
	/// <returns>
	/// A <see cref="ValueTask{TResult}"/> containing a <see cref="Result{TSuccess, TError}"/> which represents the outcome of the schema creation.
	/// The result may include the created schema version descriptor or an error in case of failure.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public ValueTask<Result<SchemaVersionDescriptor, CreateSchemaError>> CreateSchema(
		SchemaName schemaName,
		string schemaDefinition,
		SchemaDataFormat dataFormat,
		CancellationToken cancellationToken = default
	) =>
		CreateSchema(
			schemaName,
			schemaDefinition,
			dataFormat,
			CompatibilityMode.None,
			"",
			[],
			cancellationToken
		);

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

		return await ServiceClient
			.GetSchemaAsync(request, cancellationToken: cancellationToken)
			.ResponseAsync
			.ToResultAsync()
			.MatchAsync(
				onSuccess: response => Result.Success<Schema, GetSchemaError>(Schema.FromProto(response.Schema)),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.NotFound         => Result.Failure<Schema, GetSchemaError>(new ErrorDetails.SchemaNotFound(m => m.With("stream", schemaName))),
							StatusCode.PermissionDenied => Result.Failure<Schema, GetSchemaError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(GetSchema), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
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
		var request = new Contracts.GetSchemaVersionRequest {
			SchemaName = schemaName
		};

		if (versionNumber.HasValue)
			request.VersionNumber = versionNumber.Value;

		return await ServiceClient
			.GetSchemaVersionAsync(request, cancellationToken: cancellationToken)
			.ResponseAsync
			.ToResultAsync()
			.MatchAsync(
				onSuccess: response => Result.Success<SchemaVersion, GetSchemaVersionError>(SchemaVersion.FromProto(response.Version)),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.NotFound         => Result.Failure<SchemaVersion, GetSchemaVersionError>(new ErrorDetails.SchemaNotFound(m => m.With("stream", schemaName))),
							StatusCode.PermissionDenied => Result.Failure<SchemaVersion, GetSchemaVersionError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(GetSchemaVersion), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
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
		var request = new Contracts.GetSchemaVersionByIdRequest {
			SchemaVersionId = schemaVersionId
		};

		return await ServiceClient
			.GetSchemaVersionByIdAsync(request, cancellationToken: cancellationToken)
			.ResponseAsync
			.ToResultAsync()
			.MatchAsync(
				onSuccess: response => Result.Success<SchemaVersion, GetSchemaVersionError>(SchemaVersion.FromProto(response.Version)),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.NotFound         => Result.Failure<SchemaVersion, GetSchemaVersionError>(new ErrorDetails.SchemaNotFound(m => m.With("schemaVersionId", schemaVersionId))),
							StatusCode.PermissionDenied => Result.Failure<SchemaVersion, GetSchemaVersionError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(GetSchemaVersionById), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
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
		var request = new Contracts.DeleteSchemaRequest {
			SchemaName = schemaName
		};

		return await ServiceClient
			.DeleteSchemaAsync(request, cancellationToken: cancellationToken)
			.ResponseAsync
			.ToResultAsync()
			.MatchAsync(
				onSuccess: _ => Result.Success<Success, DeleteSchemaError>(Success.Instance),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.NotFound         => Result.Failure<Success, DeleteSchemaError>(new ErrorDetails.SchemaNotFound(m => m.With("schemaName", schemaName))),
							StatusCode.PermissionDenied => Result.Failure<Success, DeleteSchemaError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(DeleteSchema), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
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
	public ValueTask<Result<SchemaVersionId, CheckSchemaCompatibilityError>> CheckSchemaCompatibility(
		SchemaIdentifier identifier, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default
	) {
		var request = identifier.IsSchemaName
			? new Contracts.CheckSchemaCompatibilityRequest { SchemaName      = identifier.AsSchemaName }
			: new Contracts.CheckSchemaCompatibilityRequest { SchemaVersionId = identifier.AsSchemaVersionId };

		request.Definition = ByteString.CopyFromUtf8(schemaDefinition);
		request.DataFormat = (Contracts.SchemaDataFormat)dataFormat;

		return ServiceClient.CheckSchemaCompatibilityAsync(request, cancellationToken: cancellationToken).ResponseAsync.ToResultAsync()
			.MatchAsync(
				onSuccess: result => result.Success is not null
					? Result.Success<SchemaVersionId, CheckSchemaCompatibilityError>(SchemaVersionId.From(result.Success.SchemaVersionId))
					: Result.Failure<SchemaVersionId, CheckSchemaCompatibilityError>(SchemaCompatibilityErrors.FromProto(result.Failure.Errors)),
				onFailure: exception => {
					if (exception is RpcException rpcEx) {
						return rpcEx.StatusCode switch {
							StatusCode.NotFound => Result.Failure<SchemaVersionId, CheckSchemaCompatibilityError>(
								new ErrorDetails.SchemaNotFound(m => m.With(identifier.IsSchemaName ? "schemaName" : "schemaVersionId", identifier))
							),
							StatusCode.PermissionDenied => Result.Failure<SchemaVersionId, CheckSchemaCompatibilityError>(new ErrorDetails.AccessDenied()),
							StatusCode.InvalidArgument  => throw KurrentException.Throw(rpcEx),
							_                           => throw KurrentException.CreateUnknown(nameof(CheckSchemaCompatibility), rpcEx)
						};
					}

					throw KurrentException.Throw(exception);
				}
			);
	}
}
