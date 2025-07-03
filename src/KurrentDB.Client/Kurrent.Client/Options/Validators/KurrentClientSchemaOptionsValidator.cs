namespace Kurrent.Client;

class KurrentClientSchemaOptionsValidator : FluentOptionsValidator<KurrentClientSchemaOptionsValidator, KurrentClientSchemaOptions> {
    public KurrentClientSchemaOptionsValidator() {
        // // Schema configuration consistency warning
        // RuleFor(options => options.AutoRegister)
        //     .Must((options, autoRegister) => autoRegister || !options.Validate)
        //     .WithMessage("Schema validation is enabled but auto-registration is disabled, which may lead to validation errors for unregistered schemas.")
        //     .WithSeverity(Severity.Warning);
        //
        // // SchemaNameStrategy validation
        // RuleFor(options => options.NameStrategy)
        //     .NotNull()
        //     .WithMessage("SchemaNameStrategy cannot be null.")
        //     .WithSeverity(Severity.Error);
    }
}
