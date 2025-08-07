namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly record struct ProjectionName {
    ProjectionName(string value) => Value = value;

    public static readonly ProjectionName None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() => ArgumentException.ThrowIfNullOrWhiteSpace(Value);

    public static ProjectionName From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public static implicit operator ProjectionName(string _) => From(_);
    public static implicit operator string(ProjectionName _) => _.ToString();
}
