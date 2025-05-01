using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema;

public record KurrentSchemaControlOptions {
    public static readonly KurrentSchemaControlOptions Default = new();

    public ISchemaNameStrategy SchemaNameStrategy { get; init; } = new MessageSchemaNameStrategy();
    public bool                AutoRegister       { get; init; } = true;
}

public class KurrentSchemaControl(KurrentSchemaControlOptions options, IMessageTypeResolver typeResolver) {
    public KurrentSchemaControl() : this(new KurrentSchemaControlOptions(), null!) { }

    KurrentSchemaControlOptions Options      { get; } = options;
    IMessageTypeResolver        TypeResolver { get; } = typeResolver;

    public Type ResolveMessageType(string schemaName, string stream, Metadata metadata) =>
        TypeResolver.ResolveType(schemaName, stream, metadata);

    public Type ResolveMessageType(EventRecord record) =>
        TypeResolver.ResolveType(record.EventType, record.EventStreamId, new Metadata()); // record.Metadata

    public ValueTask<RegisteredSchema> GetOrRegisterSchema(SchemaInfo schemaInfo, Type messageType, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Schema registration is not implemented.");

    public SchemaInfo CreateSchemaInfo(string stream, Type type, SchemaDataFormat dataFormat) =>
        new(Options.SchemaNameStrategy.GenerateSchemaName(type, stream), dataFormat);

    public SchemaInfo CreateSchemaInfo(Type type, SchemaDataFormat dataFormat) =>
        new(Options.SchemaNameStrategy.GenerateSchemaName(type, ""), dataFormat);
}
