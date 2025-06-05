// ReSharper disable CheckNamespace

using Google.Protobuf;
using Grpc.Core;
using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using static KurrentDB.Protocol.Registry.V2.SchemaRegistryService;
using Contracts = KurrentDB.Protocol.Registry.V2;

namespace Kurrent.Client;

public class KurrentRegistryClient {
	internal KurrentRegistryClient(CallInvoker invoker) =>
		ServiceClient = new SchemaRegistryServiceClient(invoker);

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
	/// A <see cref="CreateSchemaResult"/> indicating the result of the schema creation operation.
	/// This can include the newly created schema version or an error specifying that the schema already exists.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public async ValueTask<CreateSchemaResult> CreateSchema(
		SchemaName schemaName, string schemaDefinition,
		SchemaDataFormat dataFormat, CompatibilityMode compatibilityMode,
		string description, Dictionary<string, string> tags,
		CancellationToken cancellationToken = default
	) {
		var request = new Contracts.CreateSchemaRequest {
			SchemaName = schemaName,
			Details = new Contracts.SchemaDetails {
				DataFormat    = (Contracts.SchemaDataFormat)dataFormat,
				Compatibility = (Contracts.CompatibilityMode)compatibilityMode, // how to do this, unspecified and the server will set the value?
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
		catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists) {
			return ErrorDetails.SchemaAlreadyExists.Value;
		}
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
	/// A <see cref="CreateSchemaResult"/> indicating the result of the schema creation operation.
	/// This can include the newly created schema version or an error specifying that the schema already exists.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public ValueTask<CreateSchemaResult> CreateSchema(
		SchemaName schemaName,
		string schemaDefinition,
		SchemaDataFormat dataFormat,
		CancellationToken cancellationToken = default
	) => CreateSchema(
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
	/// A <see cref="GetSchemaResult"/> representing the retrieved schema details or an error indicating the schema was not found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if an error occurs while trying to retrieve the schema, including issues with network connectivity or invalid input.
	/// </exception>
	public async ValueTask<GetSchemaResult> GetSchema(SchemaName schemaName, CancellationToken cancellationToken = default) {
		var request = new Contracts.GetSchemaRequest {
			SchemaName = schemaName
		};

		try {
			var response = await ServiceClient
				.GetSchemaAsync(request, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return Schema.FromProto(response.Schema);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) {
			return ErrorDetails.SchemaNotFound.Value;
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
	/// A <see cref="GetSchemaVersionResult"/> containing either the schema version details or an error indicating that the schema was not found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if an error occurs during the operation, such as network issues or invalid input.
	/// </exception>
	public async ValueTask<GetSchemaVersionResult> GetSchemaVersion(SchemaName schemaName, int? versionNumber = null, CancellationToken cancellationToken = default) {
		var request = new Contracts.GetSchemaVersionRequest {
			SchemaName = schemaName
		};

		if (versionNumber.HasValue)
			request.VersionNumber = versionNumber.Value;

		try {
			var response = await ServiceClient
				.GetSchemaVersionAsync(request, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return SchemaVersion.FromProto(response.Version);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) {
			return ErrorDetails.SchemaNotFound.Value;
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
	/// A <see cref="GetSchemaVersionResult"/> containing the requested schema version details or an error if the schema version is not found.
	/// </returns>
	/// <exception cref="RpcException">
	/// Throws an exception in case of communication errors with the schema registry service.
	/// </exception>
	public async ValueTask<GetSchemaVersionResult> GetSchemaVersionById(SchemaVersionId schemaVersionId, CancellationToken cancellationToken = default) {
		var request = new Contracts.GetSchemaVersionByIdRequest {
			SchemaVersionId = schemaVersionId
		};

		try {
			var response = await ServiceClient
				.GetSchemaVersionByIdAsync(request, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return SchemaVersion.FromProto(response.Version);
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) {
			return ErrorDetails.SchemaNotFound.Value;
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
	/// A <see cref="DeleteSchemaResult"/> indicating the result of the schema deletion operation.
	/// The result can either indicate success or specify that the schema was not found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if the operation encounters an error, such as a network issue or invalid input.
	/// </exception>
	public async ValueTask<DeleteSchemaResult> DeleteSchema(SchemaName schemaName, CancellationToken cancellationToken = default) {
		var request = new Contracts.DeleteSchemaRequest {
			SchemaName = schemaName
		};

		try {
			await ServiceClient
				.DeleteSchemaAsync(request, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return Success.Value;
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) {
			return ErrorDetails.SchemaNotFound.Value;
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
	/// A <see cref="CheckSchemaCompatibilityResult"/> object representing the result of the compatibility check.
	/// This result can be one of the following: a compatible schema version ID, a list of compatibility errors,
	/// or an indication that the schema was not found.
	/// </returns>
	/// <exception cref="Exception">
	/// Throws an exception if there is an error during the compatibility check process.
	/// </exception>
	public async ValueTask<CheckSchemaCompatibilityResult> CheckSchemaCompatibility(SchemaIdentifier identifier, string schemaDefinition, SchemaDataFormat dataFormat, CancellationToken cancellationToken = default) {
		var request = identifier.IsSchemaName
			? new Contracts.CheckSchemaCompatibilityRequest { SchemaName      = identifier.AsSchemaName }
			: new Contracts.CheckSchemaCompatibilityRequest { SchemaVersionId = identifier.AsSchemaVersionId };

		request.Definition = ByteString.CopyFromUtf8(schemaDefinition);
		request.DataFormat = (Contracts.SchemaDataFormat)dataFormat;

		try {
			var response = await ServiceClient
				.CheckSchemaCompatibilityAsync(request, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			return response.ResultCase switch {
				Contracts.CheckSchemaCompatibilityResponse.ResultOneofCase.Success => SchemaVersionId.From(response.Success.SchemaVersionId),
				Contracts.CheckSchemaCompatibilityResponse.ResultOneofCase.Failure => SchemaCompatibilityErrors.FromProto(response.Failure.Errors),
				_                                                                  => ErrorDetails.SchemaNotFound.Value
			};
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound) {
			return ErrorDetails.SchemaNotFound.Value;
		}
	}
}
