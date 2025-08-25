namespace Kurrent.Client.Users;

[PublicAPI]
public readonly record struct LoginName {
    LoginName(string value) => Value = value;

    public static readonly LoginName None = new("");

    public string Value { get; }

    public override string ToString() => Value;

    public void ThrowIfInvalid() => ArgumentException.ThrowIfNullOrWhiteSpace(Value);

    public static LoginName From(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new(value);
    }

    public static implicit operator LoginName(string _) => From(_);
    public static implicit operator string(LoginName _) => _.ToString();
}
