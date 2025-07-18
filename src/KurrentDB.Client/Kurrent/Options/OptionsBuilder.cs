using Kurrent.Client;

namespace Kurrent;

/// <summary>
/// Base class for all option builders that provides the core transformation mechanism.
/// </summary>
/// <typeparam name="TBuilder">The concrete builder type for fluent chaining</typeparam>
/// <typeparam name="TOptions">The options type being built</typeparam>
public abstract class OptionsBuilder<TBuilder, TOptions>
    where TBuilder : OptionsBuilder<TBuilder, TOptions>, new()
    where TOptions : class, new() {

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionsBuilder{TBuilder, TOptions}"/> class with default options.
    /// </summary>
    protected OptionsBuilder(TOptions options) => Transforms.Add(_ => options);

    List<Func<TOptions, TOptions>> Transforms { get; } = [];

    /// <summary>
    /// Applies a transformation and returns the same builder instance for chaining.
    /// </summary>
    public TBuilder With(Func<TOptions, TOptions> transformation) {
        Transforms.Add(transformation);
        return (TBuilder)this;
    }

    /// <summary>
    /// Configures options using a sub-builder.
    /// </summary>
    /// <typeparam name="TSubBuilder">The type of sub-builder to create.</typeparam>
    /// <param name="configure">Action to configure the sub-builder</param>
    /// <returns>The same builder instance for chaining</returns>
    protected TBuilder WithBuilder<TSubBuilder>(Action<TSubBuilder> configure) where TSubBuilder : OptionsBuilder<TSubBuilder, TOptions>, new() =>
        With(options => new TSubBuilder().WithInitialOptions(options).With(configure).GetOptions());

    /// <summary>
    /// Builds the final options instance after applying all configurations.
    /// </summary>
    [PublicAPI]
    public TOptions Build() => OnBuild(GetOptions());

    /// <summary>
    /// Hook for finalizing the options after all transformations are applied.
    /// This method can be overridden in derived classes to perform additional validation or final adjustments.
    /// The default implementation simply returns the options as is.
    /// </summary>
    /// <remarks>
    /// This method is called after all transformations have been applied, so it can assume the options
    /// are in a consistent state.
    /// </remarks>
    protected virtual TOptions OnBuild(TOptions options) => options;

    /// <summary>
    /// Initializes the builder with the provided options, resetting any existing transformations.
    /// Used internally with sub builders.
    /// </summary>
    TBuilder WithInitialOptions(TOptions options) {
        Transforms.Clear();
        Transforms.Add(_ => options);
        return (TBuilder)this;
    }

    /// <summary>
    /// Builds the final options instance by applying all accumulated transformations.
    /// </summary>
    TOptions GetOptions() =>
        Transforms.Aggregate(new TOptions(), (options, transform) => transform(options));
}
