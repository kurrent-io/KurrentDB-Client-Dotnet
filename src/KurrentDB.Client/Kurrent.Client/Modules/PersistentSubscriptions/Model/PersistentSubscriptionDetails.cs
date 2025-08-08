namespace Kurrent.Client.PersistentSubscriptions;

public partial record PersistentSubscriptionDetails {
    public string                                              Source      { get; init; } = null!;
    public SubscriptionGroupName                               Group       { get; init; } = null!;
    public string                                              Status      { get; init; } = null!;
    public IReadOnlyList<PersistentSubscriptionConnectionInfo> Connections { get; init; } = null!;
    public PersistentSubscriptionStats                         Stats       { get; init; } = null!;
    public PersistentSubscriptionSettings?                     Settings    { get; init; }
}
