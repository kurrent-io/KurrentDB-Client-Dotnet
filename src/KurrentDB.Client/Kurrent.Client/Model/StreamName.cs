namespace Kurrent.Client.Model;

[PublicAPI]
public readonly record struct StreamName {
    StreamName(string value) => Value = value;

    public static readonly StreamName None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public static StreamName From(string value) {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Stream '{value}' is not valid.", nameof(value))
            : new(value);
    }

    public static implicit operator StreamName(string _)  => From(_);
    public static implicit operator string(StreamName _) => _.ToString();
}
