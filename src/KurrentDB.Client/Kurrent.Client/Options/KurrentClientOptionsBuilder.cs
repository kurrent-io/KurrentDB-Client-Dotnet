#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

using Grpc.Core.Interceptors;
using Kurrent.Client.Schema;
using Kurrent.Client.Streams;
using Microsoft.Extensions.Logging;

namespace Kurrent.Client;

/// <summary>
/// Builder for creating <see cref="KurrentClientOptions"/> instances in a fluent manner.
/// </summary>
/// <remarks>
/// <para>
/// Provides a fluent API for configuring all aspects of KurrentDB client options,
/// including connection settings, security, resilience strategies, and schema configuration.
/// </para>
/// <para>
/// Start with <see cref="Create"/> for default options or <see cref="FromConnectionString"/> to
/// initialize from a connection string.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create with defaults and customize
/// var options = KurrentClientOptionsBuilder.Create()
///     .ConfigureConnection(builder => builder
///         .WithConnectionName("my-service")
///         .WithEndpoint("kurrentdb.example.com", 2113)
///         .WithSecurityOptions(KurrentClientSecurityOptions.MutualTls(
///             clientCertPath: "/path/to/client.crt",
///             clientKeyPath: "/path/to/client.key")))
///     .Build();
///
/// // Create from connection string
/// var options = KurrentClientOptionsBuilder
///     .FromConnectionString("kurrentdb://username:password@localhost:2113?tls=true")
///     .WithResilience(KurrentClientResilienceOptions.HighAvailability)
///     .Build();
/// </code>
/// </example>
[PublicAPI]
public sealed class KurrentClientOptionsBuilder : OptionsBuilder<KurrentClientOptionsBuilder, KurrentClientOptions> {
    public KurrentClientOptionsBuilder(KurrentClientOptions options) : base(options) { }
    public KurrentClientOptionsBuilder() : base(new KurrentClientOptions()) { }

    /// <summary>
    /// Builds the final KurrentClientOptions instance after applying all configurations and validating them.
    /// </summary>
    /// <returns>A configured KurrentClientOptions instance.</returns>
    protected override KurrentClientOptions OnBuild(KurrentClientOptions options) {
        options.EnsureOptionsAreValid();
        return options;

        // // might not need to do this anymore, double check.
        // return options with {
        //     Endpoints = options.Endpoints.ToArray(),
        //     Interceptors = options.Interceptors.ToArray()
        // };
    }

    /// <summary>
    /// Creates a new <see cref="KurrentClient"/> instance using the configured options.
    /// </summary>
    /// <returns></returns>
    public KurrentClient CreateClient() => KurrentClient.Create(Build());

    // /// <summary>
    // /// Creates a builder with default options.
    // /// </summary>
    // /// <returns>A new KurrentClientOptionsBuilder instance with default settings.</returns>
    // public static KurrentClientOptionsBuilder Create() => new();

    // /// <summary>
    // /// Creates a builder initialized from a connection string.
    // /// </summary>
    // /// <param name="connectionString">The connection string to parse.</param>
    // /// <returns>A new KurrentClientOptionsBuilder instance configured according to the connection string.</returns>
    // /// <exception cref="ArgumentNullException">Thrown when connectionString is null.</exception>
    // /// <exception cref="ArgumentException">Thrown when connectionString is invalid or empty.</exception>
    // public static KurrentClientOptionsBuilder FromConnectionString(string connectionString) {
    //     ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
    //     return new KurrentClientOptionsBuilder(KurrentClientOptions.Create(connectionString));
    // }

    /// <summary>
    /// Creates an options builder initialized from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    /// <returns>A new KurrentClientOptionsBuilder instance configured according to the connection string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when connectionString is null.</exception>
    /// <exception cref="ArgumentException">Thrown when connectionString is invalid or empty.</exception>
    public KurrentClientOptionsBuilder WithConnectionString(string connectionString) =>
        KurrentClientOptions.Parse(connectionString).GetBuilder();

    public KurrentClientOptionsBuilder ConfigureConnection(Action<ConnectionOptionsBuilder> configure) =>
        WithBuilder(configure);

    /// <summary>
    /// Sets the resilience options.
    /// </summary>
    /// <param name="resilienceOptions">The resilience configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithResilience(KurrentClientResilienceOptions resilienceOptions) =>
        With(options => options with { Resilience = resilienceOptions });

    /// <summary>
    /// Configures the existing resilience options.
    /// </summary>
    /// <param name="configure">
    /// A function to configure the existing <see cref="KurrentClientResilienceOptions"/>.
    /// </param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder ConfigureResilience(Func<KurrentClientResilienceOptions, KurrentClientResilienceOptions> configure) =>
        With(options => options with { Resilience = configure(options.Resilience) });

    public KurrentClientOptionsBuilder WithMessages(Action<MessageMappingBuilder> configure) {
		ArgumentNullException.ThrowIfNull(configure);
		return With(options => options with { Mapper = new MessageMappingBuilder().With(configure).Build() });
    }

    /// <summary>
    /// Sets the interceptors.
    /// </summary>
    /// <param name="interceptors">The gRPC interceptors to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithInterceptors(params Interceptor[] interceptors) =>
        With(options => options with { Interceptors = interceptors });

    /// <summary>
    /// Sets the metadata decoder.
    /// </summary>
    /// <param name="decoder">The metadata decoder to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithCustomMetadataDecoder(MetadataDecoder decoder) =>
        With(options => options with { MetadataDecoder = decoder });

    /// <summary>
    /// Sets the logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithLoggerFactory(ILoggerFactory loggerFactory) =>
        With(options => options with { LoggerFactory = loggerFactory });

    /// <summary>
    /// Sets the schema options.
    /// </summary>
    /// <param name="schemaOptions">The schema configuration options.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public KurrentClientOptionsBuilder WithSchema(KurrentClientSchemaOptions schemaOptions) =>
        With(options => options with { Schema = schemaOptions });
}
