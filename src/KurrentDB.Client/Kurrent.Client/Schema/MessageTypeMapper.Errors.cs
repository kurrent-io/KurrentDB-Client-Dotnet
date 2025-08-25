using Kurrent.Client.Registry;

namespace Kurrent.Client.Schema;

public static class MessageTypeMapperErrors {
    public readonly record struct MessageTypeAlreadyMapped(string SchemaName, Type AttemptedMessageType, Type RegisteredMessageType) {
        public override string ToString() => $"Message '{AttemptedMessageType.Name}' is already mapped with the name '{SchemaName}' as '{RegisteredMessageType.FullName}'";
    }

    public readonly record struct SchemaNameMapNotFound(string SchemaName) {
        public override string ToString() => $"Schema '{SchemaName}' not mapped";
    }

    public readonly record struct MessageTypeMapNotFound(Type MessageType) {
        public override string ToString() => $"Message '{MessageType.Name}' not mapped";
    }

    public readonly record struct MessageTypeConflict(SchemaName SchemaName, Type MappedType, Type AttemptedType) {
        public override string ToString() => $"Schema '{SchemaName}' is already mapped with type '{MappedType.FullName}' "
                                           + $"but attempted to use with incompatible type '{AttemptedType.FullName}'";
    }

    public readonly record struct MessageTypeResolution(SchemaName SchemaName) {
        public override string ToString() => $"Schema '{SchemaName}' not mapped and does not match any known type";
    }
}
