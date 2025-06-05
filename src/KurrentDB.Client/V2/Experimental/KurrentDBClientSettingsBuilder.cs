// using Grpc.Core;
// using Grpc.Core.Interceptors;
// using Microsoft.Extensions.Logging;
//
// namespace KurrentDB.Client;
//
// /// <summary>
// /// A builder for creating instances of <see cref="KurrentDBClientSettings"/> with a fluent API.
// /// </summary>
// public class KurrentDBClientSettingsBuilder {
// 	readonly KurrentDBClientSettings _settings;
//
// 	/// <summary>
// 	/// Creates a new instance of the <see cref="KurrentDBClientSettingsBuilder"/>.
// 	/// </summary>
// 	public KurrentDBClientSettingsBuilder(KurrentDBClientSettings? settings = null) =>
// 		_settings = settings ?? new KurrentDBClientSettings();
//
// 	/// <summary>
// 	/// Creates a new instance of the <see cref="KurrentDBClientSettingsBuilder"/> with settings initialized from a connection string.
// 	/// </summary>
// 	/// <param name="connectionString">The connection string to parse.</param>
// 	public static KurrentDBClientSettingsBuilder FromConnectionString(string connectionString) {
// 		var settings = KurrentDBConnectionString.Parse(connectionString).ToClientSettings();
// 		return new KurrentDBClientSettingsBuilder(settings);
// 	}
//
// 	/// <summary>
// 	/// Sets the list of interceptors to use.
// 	/// </summary>
// 	/// <param name="interceptors">The interceptors to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithInterceptors(IEnumerable<Interceptor> interceptors) {
// 		_settings.Interceptors = interceptors;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the connection name.
// 	/// </summary>
// 	/// <param name="connectionName">The name of the connection.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithConnectionName(string connectionName) {
// 		if (string.IsNullOrWhiteSpace(connectionName))
// 			throw new ArgumentException("Connection name cannot be null or whitespace.", nameof(connectionName));
//
// 		_settings.ConnectionName = connectionName;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the HTTP message handler factory.
// 	/// </summary>
// 	/// <param name="handlerFactory">The HTTP message handler factory to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithHttpMessageHandler(Func<HttpMessageHandler> handlerFactory) {
// 		_settings.CreateHttpMessageHandler = handlerFactory;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the logger factory.
// 	/// </summary>
// 	/// <param name="loggerFactory">The logger factory to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithLoggerFactory(ILoggerFactory loggerFactory) {
// 		_settings.LoggerFactory = loggerFactory;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the channel credentials.
// 	/// </summary>
// 	/// <param name="credentials">The channel credentials to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithChannelCredentials(ChannelCredentials credentials) {
// 		_settings.ChannelCredentials = credentials;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the operation options.
// 	/// </summary>
// 	/// <param name="options">The operation options to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithOperationOptions(KurrentDBClientOperationOptions options) {
// 		_settings.OperationOptions = options;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Configures operation options for the KurrentDB client.
// 	/// </summary>
// 	/// <param name="configure">An action to configure the <see cref="KurrentDBClientOperationOptions"/> instance.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithOperationOptions(Action<KurrentDBClientOperationOptions> configure) {
// 		var options = new KurrentDBClientOperationOptions();
// 		configure(options);
// 		_settings.OperationOptions = options;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the connectivity settings.
// 	/// </summary>
// 	/// <param name="connectivitySettings">The connectivity settings to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithConnectivitySettings(KurrentDBClientConnectivitySettings connectivitySettings) {
// 		_settings.ConnectivitySettings = connectivitySettings;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Configures the connectivity settings for the client using a provided action.
// 	/// </summary>
// 	/// <param name="configure">A delegate to configure the <see cref="KurrentDBClientConnectivitySettings"/> instance.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithConnectivitySettings(Action<KurrentDBClientConnectivitySettings> configure) {
// 		var settings = new KurrentDBClientConnectivitySettings();
// 		configure(settings);
// 		_settings.ConnectivitySettings = settings;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the default user credentials.
// 	/// </summary>
// 	/// <param name="credentials">The default user credentials to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithDefaultCredentials(UserCredentials credentials) {
// 		_settings.DefaultCredentials = credentials;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Sets the default deadline for calls.
// 	/// </summary>
// 	/// <param name="deadline">The default deadline to use.</param>
// 	/// <returns>The current instance of <see cref="KurrentDBClientSettingsBuilder"/> for fluent configuration.</returns>
// 	public KurrentDBClientSettingsBuilder WithDefaultDeadline(TimeSpan deadline) {
// 		_settings.DefaultDeadline = deadline;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Configures retry settings.
// 	/// </summary>
// 	/// <param name="retrySettings">The retry settings to use.</param>
// 	/// <returns>The builder for method chaining.</returns>
// 	public KurrentDBClientSettingsBuilder WithRetrySettings(KurrentDBClientRetrySettings retrySettings) {
// 		_settings.RetrySettings = retrySettings;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Disables retries for gRPC operations.
// 	/// </summary>
// 	/// <returns>The builder for method chaining.</returns>
// 	public KurrentDBClientSettingsBuilder WithNoRetry() {
// 		_settings.RetrySettings = KurrentDBClientRetrySettings.NoRetry;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Configures a custom retry policy.
// 	/// </summary>
// 	/// <param name="configure">Action to configure retry settings.</param>
// 	/// <returns>The builder for method chaining.</returns>
// 	public KurrentDBClientSettingsBuilder WithRetry(Action<KurrentDBClientRetrySettings> configure) {
// 		var settings = new KurrentDBClientRetrySettings();
// 		configure(settings);
// 		_settings.RetrySettings = settings;
// 		return this;
// 	}
//
// 	/// <summary>
// 	/// Builds the <see cref="KurrentDBClientSettings"/> instance.
// 	/// </summary>
// 	/// <returns>The configured settings instance.</returns>
// 	public KurrentDBClientSettings Build() => _settings;
// }
