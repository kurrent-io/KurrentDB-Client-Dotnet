namespace KurrentDB.Client;
#pragma warning disable 1591

public record ServerCapabilities(
    string Version = "0.0.0",
    bool SupportsBatchAppend = false,
    bool SupportsPersistentSubscriptionsToAll = false,
    bool SupportsPersistentSubscriptionsGetInfo = false,
    bool SupportsPersistentSubscriptionsRestartSubsystem = false,
    bool SupportsPersistentSubscriptionsReplayParked = false,
    bool SupportsPersistentSubscriptionsList = false,
    bool SupportsSchemaRegistry = false
);
