namespace Kurrent.Client.PersistentSubscriptions;

[PublicAPI]
public readonly record struct SubscriptionGroupName {
    SubscriptionGroupName(string value) => Value = value;

    public static readonly SubscriptionGroupName None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() => ArgumentException.ThrowIfNullOrWhiteSpace(Value);

    public static SubscriptionGroupName From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public static implicit operator SubscriptionGroupName(string _) => From(_);
    public static implicit operator string(SubscriptionGroupName _) => _.ToString();
}
