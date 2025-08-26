namespace Kurrent.Client;

[PublicAPI]
public abstract record KurrentResultError<TSelf> : IResultError where TSelf : KurrentResultError<TSelf> {
    static readonly KurrentOperationErrorAttribute Info = KurrentOperationErrorAttribute.GetRequiredAttribute(typeof(TSelf));

    protected KurrentResultError(Metadata? errorData = null) =>
        ErrorData = (errorData ?? []).Lock();

    protected KurrentResultError(Action<Metadata> configureErrorData)
        : this(new Metadata().Transform(configureErrorData)) { }

    protected KurrentResultError(string errorMessage, Metadata? errorData = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        ErrorMessage = errorMessage;
        ErrorData    = (errorData ?? []).Lock();
    }

    protected KurrentResultError(string errorCode, string errorMessage, ErrorSeverity errorSeverity, Metadata errorData) {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        ErrorCode     = errorCode;
        ErrorMessage  = errorMessage;
        ErrorSeverity = errorSeverity;
        ErrorData     = errorData.Lock();
    }

    public virtual string        ErrorCode     { get; } = Info.Annotations.Code;
    public virtual string        ErrorMessage  { get; } = Info.Annotations.Message;
    public virtual ErrorSeverity ErrorSeverity { get; } = Info.Annotations.Severity;

    public Metadata ErrorData { get; }

    public virtual Exception CreateException(Exception? innerException = null) =>
        new KurrentException(ErrorCode, ErrorMessage, ErrorSeverity, ErrorData, innerException);

    public override string ToString() => ErrorMessage;
}
