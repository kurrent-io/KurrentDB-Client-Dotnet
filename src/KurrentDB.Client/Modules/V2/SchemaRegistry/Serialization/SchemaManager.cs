using System.Collections.Concurrent;
using JetBrains.Annotations;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.SchemaRegistry.Serialization;

/// <summary>
/// Exception thrown when a schema validation fails.
/// </summary>
public class SchemaValidationException(SchemaDataFormat dataFormat, SchemaIdentifier schemaIdentifier, Type messageType, IReadOnlyList<SchemaCompatibilityError> validationErrors)
	: Exception(FormatMessage(messageType, schemaIdentifier, validationErrors)) {
	public SchemaDataFormat                        DataFormat       { get; } = dataFormat;
	public Type                                    MessageType      { get; } = messageType;
	public SchemaIdentifier                        SchemaIdentifier { get; } = schemaIdentifier;
	public IReadOnlyList<SchemaCompatibilityError> ValidationErrors { get; } = validationErrors;

	static string FormatMessage(Type messageType, SchemaIdentifier schemaIdentifier, IReadOnlyList<SchemaCompatibilityError> validationErrors) {
		var schemaIdentifierText = schemaIdentifier.Match(
			schemaName => $"schema '{schemaName}'",
			schemaVersionId => $"schema version '{schemaVersionId}'"
		);

		return $"Schema validation failed for message type '{messageType.FullName}' with {schemaIdentifierText}."
		     + $"Found {validationErrors.Count} errors:{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors.Select(error => $" - {error}"))}";
	}
}

/// <summary>
/// Exception thrown when a schema cannot be found in the registry.
/// </summary>
public class SchemaNotFoundException(SchemaIdentifier schemaIdentifier) : Exception(FormatMessage(schemaIdentifier)) {
	public SchemaIdentifier SchemaIdentifier { get; } = schemaIdentifier;

	static string FormatMessage(SchemaIdentifier schemaIdentifier) =>
		schemaIdentifier.Match(
			schemaName      => $"Schema '{schemaName}' was not found in the registry.",
			schemaVersionId => $"Schema version '{schemaVersionId}' was not found in the registry."
		);
}

[PublicAPI]
public class SchemaManager(KurrentRegistryClient schemaRegistry, ISchemaExporter schemaExporter, MessageTypeMapper typeMapper) {
	KurrentRegistryClient SchemaRegistry { get; } = schemaRegistry;
	ISchemaExporter       SchemaExporter { get; } = schemaExporter;
	MessageTypeMapper     TypeMapper     { get; } = typeMapper;

	ConcurrentDictionary<Type, List<SchemaVersionDescriptor>> CompatibleVersions { get; } = new();

	#region Serialize

	public async ValueTask<(SchemaName SchemaName, SchemaVersionId SchemaVersionId)> RegisterOrValidateSchema(Type messageType, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
		if (policy is { AutoRegisterSchemas: true, DataFormat: SchemaDataFormat.Json })
			return await HandleServerRegistration(messageType, policy, ct);

		if (policy is { ValidateSchemas: true, DataFormat: SchemaDataFormat.Json })
			return await HandleServerValidation(messageType, policy, ct);

		return HandleLocalRegistration(messageType, policy);
	}

	async ValueTask<(SchemaName SchemaName, SchemaVersionId SchemaVersionId)> HandleServerRegistration(Type messageType,  ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
		var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);

		if (!mapped)
			schemaName = policy.GetSchemaName(messageType);

		var versionInfo = await TryRegisterSchema(schemaName, messageType, policy.DataFormat, ct)
			.ConfigureAwait(false);

		if (!mapped)
			TypeMapper.TryMap(schemaName, messageType);

		return (schemaName, versionInfo.VersionId);
	}

	async ValueTask<(SchemaName schemaName, SchemaVersionId lastSchemaVersionId)> HandleServerValidation(Type messageType, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
		var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);

		if (!mapped)
			schemaName = policy.GetSchemaName(messageType);

		var lastSchemaVersionId = await EnsureSchemaCompatibility(schemaName, messageType, policy.DataFormat, ct)
			.ConfigureAwait(false);

		if (!mapped)
			TypeMapper.TryMap(schemaName, messageType);

		return (schemaName, lastSchemaVersionId);
	}

	(SchemaName schemaName, SchemaVersionId lastSchemaVersionId) HandleLocalRegistration(Type messageType, ResolvedSchemaRegistryPolicy policy) {
		if (policy.AutoMapMessages) {
			var mapped = TypeMapper.TryGetSchemaName(messageType, out var schemaName);
			if (!mapped) {
				schemaName = policy.GetSchemaName(messageType);
				TypeMapper.TryMap(schemaName, messageType);
			}

			return (schemaName, SchemaVersionId.None);
		}
		else if (TypeMapper.TryGetSchemaName(messageType, out var schemaName))
			return (schemaName, SchemaVersionId.None);
		else
			throw new AutoRegistrationDisabledException(policy.DataFormat, messageType);
	}

	#endregion

	#region Deserialize

	public async ValueTask<(Type MessageType, SchemaVersionId SchemaVersionId)> ValidateAndEnsureSchemaCompatibility(RecordSchemaInfo schemaInfo, ResolvedSchemaRegistryPolicy policy, CancellationToken ct) {
		var messageType = TypeMapper.GetOrResolveMessageType(schemaInfo.SchemaName);

		if (!policy.ValidateSchemas)
			return (messageType, SchemaVersionId.None);

		if (schemaInfo.HasSchemaVersionId) {
			await EnsureSchemaCompatibility(schemaInfo.SchemaVersionId, messageType, schemaInfo.DataFormat, ct).ConfigureAwait(false);

			// it was already set so we don't need to do anything and dont return the version id
			return (messageType, SchemaVersionId.None);
		}

		// fallback behaviour for backwards compatibility
		var lastSchemaVersionId = await EnsureSchemaCompatibility(schemaInfo.SchemaName, messageType, schemaInfo.DataFormat, ct).ConfigureAwait(false);

		return (messageType, lastSchemaVersionId);
	}

	#endregion

	/// <summary>
	/// Attempts to register a schema for the specified schema name and message type in the schema registry.
	/// If the schema already exists, the corresponding version information is returned; otherwise, a new schema is created.
	/// </summary>
	/// <param name="schemaName">The name of the schema to register.</param>
	/// <param name="messageType">The type to serialize and register in the schema registry.</param>
	/// <param name="dataFormat">The data format (e.g., Json, Protobuf, etc.) of the schema.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests during the operation.</param>
	/// <returns>The version descriptor of the schema, including its unique ID and version number.</returns>
	public async ValueTask<SchemaVersionDescriptor> TryRegisterSchema(SchemaName schemaName, Type messageType, SchemaDataFormat dataFormat, CancellationToken cancellationToken) {
		if (schemaName == SchemaName.None)
			throw new ArgumentNullException(nameof(schemaName), "Schema name cannot be None");

		if (messageType == Type.Missing.GetType())
			throw new ArgumentNullException(nameof(messageType), "Message type cannot be Missing");

		if (dataFormat == SchemaDataFormat.Unspecified)
			throw new ArgumentNullException(nameof(dataFormat), "Data format cannot be Unspecified");

		if (CompatibleVersions.TryGetValue(messageType, out var versions))
			return versions.Last();

		var getSchemaVersionResult = await SchemaRegistry
			.GetSchemaVersion(schemaName, null, cancellationToken)
			.ConfigureAwait(false);

		if (getSchemaVersionResult.Value is SchemaVersion version) {
			var versionInfo = new SchemaVersionDescriptor(version.VersionId, version.VersionNumber);
			CompatibleVersions.TryAdd(messageType, [versionInfo]);
			return versionInfo;
		}
		else {
			var definition = SchemaExporter.Export(messageType, dataFormat);

			var createSchemaResult = await SchemaRegistry
				.CreateSchema(schemaName, definition, dataFormat, cancellationToken)
				.ConfigureAwait(false);

			return await createSchemaResult.Match(
				versionInfo => {
					// edge case: the schema was created by another client between checking and creating
					var added = CompatibleVersions.TryAdd(messageType, [versionInfo]);
					if (added) {
						return new(versionInfo);
					}
					else
						return TryRegisterSchema(schemaName, messageType, dataFormat, cancellationToken);
				},
				// edge case: the schema was created by another client between checking and creating
				_ => TryRegisterSchema(schemaName, messageType, dataFormat, cancellationToken)
			);
		}
	}

	/// <summary>
	/// Used for messages that were appended using the new client.
	/// </summary>
	/// <param name="schemaVersionId"></param>
	/// <param name="messageType"></param>
	/// <param name="dataFormat"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<SchemaVersionId> EnsureSchemaCompatibility(SchemaVersionId schemaVersionId, Type messageType, SchemaDataFormat dataFormat, CancellationToken cancellationToken) {
		// Check compatible versions cache and return the last version id if found
		if (TryGetLastSchemaVersion(schemaVersionId, out var foundSchemaVersion))
			return foundSchemaVersion.VersionId;

		var definition = SchemaExporter.Export(messageType, dataFormat);

		var result = await SchemaRegistry
			.CheckSchemaCompatibility(schemaVersionId, definition, dataFormat, cancellationToken)
			.ConfigureAwait(false);

		return result.Match(
			lastSchemaVersionId => {
				// its impossible to update because we start by checking the cache
				CompatibleVersions.AddOrUpdate(
					messageType,
					static (_, state) => [state.Version, state.LastVersion],
					static (_, versions, state) => {
						// just so that the last version can represent the latest one.
						// might end up not being that useful, but it is a good idea to have it
						// if the guids were sortable that would be even better and none of this
						// would be needed
						versions.Insert(versions.Count - 1, state.Version);
						return versions;
					},
					(
						LastVersion: new SchemaVersionDescriptor(lastSchemaVersionId, 0),
						Version: new SchemaVersionDescriptor(schemaVersionId, 0)
					)
				);

				return lastSchemaVersionId;
			},
			errors => throw new SchemaValidationException(dataFormat, schemaVersionId, messageType, errors.Errors),
			_ => throw new SchemaNotFoundException(schemaVersionId)
		);

		// Attempts to retrieve the last schema version ID that is compatible with the provided schema version ID.
		bool TryGetLastSchemaVersion(SchemaVersionId schemaVersionId, out SchemaVersionDescriptor lastSchemaVersion) {
			foreach (var entry in CompatibleVersions) {
				if (entry.Value.Any(x => x.VersionId == schemaVersionId)) {
					lastSchemaVersion = entry.Value.Last();
					return true;
				}
			}

			lastSchemaVersion = SchemaVersionDescriptor.None;
			return false;
		}
	}

	/// <summary>
	/// Used for messages that were not appended using the new client.
	/// </summary>
	/// <param name="schemaName"></param>
	/// <param name="messageType"></param>
	/// <param name="dataFormat"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async ValueTask<SchemaVersionId> EnsureSchemaCompatibility(SchemaName schemaName, Type messageType, SchemaDataFormat dataFormat, CancellationToken cancellationToken) {
		if (CompatibleVersions.TryGetValue(messageType, out var versions))
			return versions.Last().VersionId;

		var definition = SchemaExporter.Export(messageType, SchemaDataFormat.Json);

		var result = await SchemaRegistry
			.CheckSchemaCompatibility(schemaName, definition, dataFormat, cancellationToken)
			.ConfigureAwait(false);

		return result.Match(
			schemaVersionId => {
				// its impossible to update because we start by checking the cache
				CompatibleVersions.TryAdd(messageType, [new SchemaVersionDescriptor(schemaVersionId, 0)]);
				return schemaVersionId;
			},
			errors => throw new SchemaValidationException(dataFormat, schemaName, messageType, errors.Errors),
			_ => throw new SchemaNotFoundException(schemaName)
		);
	}
}
