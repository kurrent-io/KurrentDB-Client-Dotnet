using Google.Protobuf;
using Kurrent.Client.Schema.NameStrategies;

namespace Kurrent.Client.Schema;

public readonly record struct MessageMap(Type MessageType, string SchemaName);

/// <summary>
/// Provides extension methods for <see cref="MessageTypeMapper"/> to support automatic message mapping.
/// </summary>
public static class MessageTypeMapperExtensions {
    /// <summary>
    /// Scans the specified namespace and automatically maps all message types within, using the provided or default schema name strategy.
    /// </summary>
    /// <param name="mapper">The message type mapper instance that performs the mapping.</param>
    /// <param name="namespacePrefix">The namespace prefix to scan for message types to be mapped.</param>
    /// <param name="nameStrategy">The schema name strategy to use for naming. If not provided, a default strategy will be used.</param>
    /// <returns>A result containing an array of <see cref="MessageMap"/> objects if successful, or a <see cref="MessageTypeMapperErrors.MessageTypeConflict"/> error in case of conflict.</returns>
    public static Result<MessageMap[], MessageTypeMapperErrors.MessageTypeConflict> AutoMap(this MessageTypeMapper mapper, string namespacePrefix, ISchemaNameStrategy? nameStrategy = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespacePrefix);

        nameStrategy ??= new MessageSchemaNameStrategy();

        return AssemblyScanner.System.Scan()
            .InstancesInNamespace(namespacePrefix)
            .Select(type => new MessageMap(type, nameStrategy.GenerateSchemaName(type)))
            .Select(msgMap => mapper
                .Map(msgMap.SchemaName, msgMap.MessageType)
                .Map(_ => msgMap))
            .Sequence()
            .Map(maps => maps.ToArray());
    }

    /// <summary>
    /// Scans the specified namespace for message types of a specific type and automatically maps them using the provided or default schema name strategy.
    /// </summary>
    /// <typeparam name="T">The type of the message to be mapped.</typeparam>
    /// <param name="mapper">The message type mapper instance used for mapping.</param>
    /// <param name="namespacePrefix">The namespace prefix to scan for message types.</param>
    /// <param name="nameStrategy">The schema name strategy to use for naming. If not provided, a default strategy will be used.</param>
    /// <returns>A result containing an array of <see cref="MessageMap"/> objects if successful, or a <see cref="MessageTypeMapperErrors.MessageTypeConflict"/> error in case of conflict.</returns>
    public static Result<MessageMap[], MessageTypeMapperErrors.MessageTypeConflict> AutoMapMessagesOf<T>(this MessageTypeMapper mapper, string namespacePrefix, ISchemaNameStrategy? nameStrategy = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(namespacePrefix);

        nameStrategy ??= new MessageSchemaNameStrategy();

        return AssemblyScanner.System.Scan()
            .InstancesInNamespace(namespacePrefix)
            .InstancesOf<T>()
            .Select(type => new MessageMap(type, nameStrategy.GenerateSchemaName(type)))
            .Select(msgMap => mapper
                .Map(msgMap.SchemaName, msgMap.MessageType)
                .Map(_ => msgMap))
            .Sequence()
            .Map(maps => maps.ToArray());
    }

    /// <summary>
    /// Scans the specified namespace for message types that implement <see cref="IMessage"/> and automatically maps them using the provided or default schema name strategy.
    /// </summary>
    /// <param name="mapper">The <see cref="MessageTypeMapper"/> instance to use for mapping.</param>
    /// <param name="namespacePrefix">The namespace prefix to filter the message types for mapping.</param>
    /// <param name="nameStrategy">The schema name strategy to use for naming. If not provided, a default strategy will be used.</param>
    /// <returns>A result containing an array of <see cref="MessageMap"/> objects if successful, or a <see cref="MessageTypeMapperErrors.MessageTypeConflict"/> error in case of conflict.</returns>
    public static Result<MessageMap[], MessageTypeMapperErrors.MessageTypeConflict> AutoMapProtobufMessages(this MessageTypeMapper mapper, string namespacePrefix, ISchemaNameStrategy? nameStrategy = null) =>
        mapper.AutoMapMessagesOf<IMessage>(namespacePrefix, nameStrategy);
}
