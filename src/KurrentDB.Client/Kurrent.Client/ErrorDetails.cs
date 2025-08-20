using Types = KurrentDB.Protocol.CoreErrorDetails.Types;

namespace Kurrent.Client;

/// <summary>
/// Provides a set of error detail types for representing specific operation failures when interacting with KurrentDB.
/// </summary>
[PublicAPI]
public static partial class ErrorDetails {
    [KurrentOperationError(typeof(Types.AccessDenied))]
    public partial record AccessDenied {
        public static AccessDenied WithDetails(string operation, string? reason = null) =>
            new($"Access denied for operation: {operation}{(reason is not null ? $" Reason: {reason}" : string.Empty)}",
                Metadata.New.With("Operation", operation).With("Reason", reason));
    }

    [KurrentOperationError(typeof(Types.NotFound))]
    public partial record NotFound {
        public static NotFound WithEntityInfo(string entityType, string entityId) =>
            new($"The specified {entityType} was not found: {entityId}",
                Metadata.New.With("EntityType", entityType).With("EntityId", entityId));
    }

    [KurrentOperationError(typeof(Types.AlreadyExists))]
    public partial record AlreadyExists {
        public static AlreadyExists WithEntityInfo(string entityType, string entityId) =>
            new($"The specified {entityType} already exists: {entityId}",
                Metadata.New.With("EntityType", entityType).With("EntityId", entityId));
    }

    [KurrentOperationError(typeof(Types.FailedPrecondition))]
    public partial record FailedPrecondition {
        public static AccessDenied WithDetails(string operation, string? reason = null) =>
            new($"Operation failed due to a precondition not being met: {operation}{(reason is not null ? $" Reason: {reason}" : string.Empty)}",
                Metadata.New.With("Operation", operation).With("Reason", reason));
    }
}
