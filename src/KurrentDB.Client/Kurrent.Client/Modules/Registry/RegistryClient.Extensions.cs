// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

using Kurrent.Client.Streams;

namespace Kurrent.Client.Registry;

public static class RegistryClientExtensions {

    public static ValueTask<Result<SchemaVersionDescriptor, CreateSchemaError>> CreateSchema(
        this RegistryClient client,
        SchemaName schema,
        string schemaDefinition,
        SchemaDataFormat dataFormat,
        CancellationToken cancellationToken = default
    ) => client.CreateSchema(schema, schemaDefinition, dataFormat, CompatibilityMode.None, "", [], cancellationToken);

    public static ValueTask<Result<SchemaVersionDescriptor, CreateSchemaError>> CreateSchema(
        this RegistryClient client,
        SchemaName schema,
        string schemaDefinition,
        SchemaDataFormat dataFormat,
        string description,
        CancellationToken cancellationToken = default
    ) => client.CreateSchema(schema, schemaDefinition, dataFormat, CompatibilityMode.None, description, [], cancellationToken);


    public static ValueTask<Result<SchemaVersionDescriptor, CreateSchemaError>> CreateSchema(
        this RegistryClient client,
        SchemaName schema,
        string schemaDefinition,
        SchemaDataFormat dataFormat,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default
    ) => client.CreateSchema(schema, schemaDefinition, dataFormat, CompatibilityMode.None, "", tags, cancellationToken);

}
