using Kurrent.Validation;

namespace Kurrent;

public interface IOptionsValidator<in TOptions> : IValidator<TOptions> where TOptions : class, IOptions;

public interface IOptions {
    Result<ValidationSuccess, ValidationError> ValidateConfig();

    void EnsureConfigIsValid();
}

public abstract record OptionsBase<TOptions, TValidator> : IOptions where TValidator : IOptionsValidator<TOptions>, new() where TOptions : class, IOptions {
    static readonly TValidator Validator = new TValidator();

    public Result<ValidationSuccess, ValidationError> ValidateConfig() => Validator.ValidateOptions((dynamic)this);

    public void EnsureConfigIsValid() => ValidateConfig().ThrowOnFailure();
}
