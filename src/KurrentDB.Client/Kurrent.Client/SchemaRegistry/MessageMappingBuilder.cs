namespace Kurrent.Client.SchemaRegistry {
    /// <summary>
    /// A builder for configuring message type mappings.
    /// </summary>
    public class MessageMappingBuilder {
        MessageTypeMapper   Mapper       { get; }      = new();
        ISchemaNameStrategy NameStrategy { get; set; } = new MessageSchemaNameStrategy();

        /// <summary>
        /// Sets the schema name strategy to use for automatic mapping.
        /// </summary>
        public MessageMappingBuilder WithNameStrategy(ISchemaNameStrategy strategy) {
            NameStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            return this;
        }

        /// <summary>
        /// Maps a message type to a schema name.
        /// </summary>
        public MessageMappingBuilder Map<T>(string? schemaName = null) {
            var type = typeof(T);
            var name = schemaName ?? NameStrategy.GenerateSchemaName(type);
            Mapper.TryMap(name, type);
            return this;
        }

        /// <summary>
        /// Attempts to map a message type to a schema name.
        /// </summary>
        public MessageMappingBuilder TryMap<T>(string? schemaName = null) {
            var type = typeof(T);
            var name = schemaName ?? NameStrategy.GenerateSchemaName(type);
            Mapper.TryMap(name, type);
            return this;
        }

        /// <summary>
        /// Automatically maps all message types in the specified namespace.
        /// </summary>
        public MessageMappingBuilder AutoMap(string namespacePrefix) {
            Mapper.AutoMap(namespacePrefix, NameStrategy);
            return this;
        }

        /// <summary>
        /// Automatically maps all message types in the specified namespace.
        /// </summary>
        public MessageMappingBuilder AutoMap<T>(string namespacePrefix) {
            Mapper.AutoMapMessagesOf<T>(namespacePrefix, NameStrategy);
            return this;
        }

        /// <summary>
        /// Automatically maps all Protobuf message types in the specified namespace.
        /// </summary>
        public MessageMappingBuilder AutoMapProtobuf(string namespacePrefix) {
            Mapper.AutoMapProtobufMessages(namespacePrefix, NameStrategy);
            return this;
        }

        internal MessageTypeMapper Build() => Mapper;
    }
}
