namespace Kurrent.Client.PersistentSubscriptions;

public partial record PersistentSubscriptionDetails {
	public string                                            EventSource { get; init; } = null!;
	public string                                            GroupName   { get; init; } = null!;
	public string                                            Status      { get; init; } = null!;
	public IEnumerable<PersistentSubscriptionConnectionInfo> Connections { get; init; } = null!;
	public PersistentSubscriptionStats                       Stats       { get; init; } = null!;
	public PersistentSubscriptionSettings?                   Settings    { get; init; }
}
