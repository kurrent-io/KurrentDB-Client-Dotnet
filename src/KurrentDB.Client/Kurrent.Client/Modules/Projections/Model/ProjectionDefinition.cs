using Jint;

namespace Kurrent.Client.Projections;

[PublicAPI]
public readonly record struct ProjectionDefinition {
    internal ProjectionDefinition(string value) => Value = value;

    public static readonly ProjectionDefinition None = new("");

    public string Value { get; internal init; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() {
        ArgumentException.ThrowIfNullOrWhiteSpace(Value);
    }

    public static ProjectionDefinition From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        try {
            return !Engine.PrepareScript(value).IsValid
                ? throw new ArgumentException("Invalid projection definition script.", nameof(value))
                : new(value);
        }
        catch (Exception ex) when (ex is not ArgumentException) {
            throw new ArgumentException("Invalid projection definition script.", nameof(value));
        }
    }

    public static implicit operator ProjectionDefinition(string _) => From(_);
    public static implicit operator string(ProjectionDefinition _) => _.ToString();
}
