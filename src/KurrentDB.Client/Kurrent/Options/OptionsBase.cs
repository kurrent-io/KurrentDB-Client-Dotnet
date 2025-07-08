using System.Reflection;
using Kurrent.Validation;

namespace Kurrent;

[PublicAPI]
public abstract record OptionsBase<TOptions, TValidator> : IOptions where TValidator : class, IOptionsValidator<TValidator, TOptions>, new() where TOptions : class, IOptions {
	static Lazy<TValidator> LazyValidator => new(() => typeof(TValidator)
        .GetField("Instance", BindingFlags.Public | BindingFlags.Static)?
        .GetValue(null) as TValidator ?? new TValidator());

    static TValidator Validator => LazyValidator.Value;

    public Result<ValidationSuccess, ValidationError> ValidateOptions() =>
        Validator.ValidateOptions((dynamic)this);

    public void EnsureOptionsAreValid() =>
        ValidateOptions().ThrowOnFailure();
}

public interface IOptionsValidator<TValidator, in TOptions> : IValidator<TOptions>
    where TOptions : class, IOptions
    where TValidator : IOptionsValidator<TValidator, TOptions>, new() {
    public string ErrorCode { get; }
}

public interface IOptions {
    Result<ValidationSuccess, ValidationError> ValidateOptions();
}
