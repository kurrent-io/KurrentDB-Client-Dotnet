namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly record struct ProjectionPartition {
    ProjectionPartition(string value) => Value = value;

    public static readonly ProjectionPartition None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() => ArgumentException.ThrowIfNullOrWhiteSpace(Value);

    public static ProjectionPartition From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public static implicit operator ProjectionPartition(string _) => From(_);
    public static implicit operator string(ProjectionPartition _) => _.ToString();
}
