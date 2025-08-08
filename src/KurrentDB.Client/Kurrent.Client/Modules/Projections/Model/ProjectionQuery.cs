namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly record struct ProjectionQuery {
    ProjectionQuery(string value) => Value = value;

    public static readonly ProjectionQuery None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() => ArgumentException.ThrowIfNullOrWhiteSpace(Value);

    public static ProjectionQuery From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public static implicit operator ProjectionQuery(string _) => From(_);
    public static implicit operator string(ProjectionQuery _) => _.ToString();
}
