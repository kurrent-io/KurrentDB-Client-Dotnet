using Grpc.Core;
using Grpc.Core.Interceptors;
using Kurrent.Client;
using Kurrent.Client.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client;

public class KurrentClientSettings {
	// public static KurrentDBClientSettingsBuilder Builder => new();

	/// <summary>
	/// The name of the connection.
	/// </summary>
	public string? ConnectionName { get; set; }

	/// <summary>
	/// An optional list of <see cref="Interceptor"/>s to use.
	/// </summary>
	public IEnumerable<Interceptor> Interceptors { get; set; } = [];

	/// <summary>
	/// An optional <see cref="HttpMessageHandler"/> factory.
	/// </summary>
	public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }

	/// <summary>
	/// An optional <see cref="ILoggerFactory"/> to use.
	/// </summary>
	public ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

	/// <summary>
	/// The optional <see cref="ChannelCredentials"/> to use when creating the <see cref="ChannelBase"/>.
	/// </summary>
	public ChannelCredentials? ChannelCredentials { get; set; }

	/// <summary>
	/// The default <see cref="KurrentDBClientOperationOptions"/> to use.
	/// </summary>
	public KurrentDBClientOperationOptions OperationOptions { get; set; } =
		KurrentDBClientOperationOptions.Default;

	/// <summary>
	/// The <see cref="KurrentDBClientConnectivitySettings"/> to use.
	/// </summary>
	public KurrentDBClientConnectivitySettings ConnectivitySettings { get; set; } =
		KurrentDBClientConnectivitySettings.Default;

	/// <summary>
	/// The optional <see cref="UserCredentials"/> to use if none have been supplied to the operation.
	/// </summary>
	public UserCredentials? DefaultCredentials { get; set; } //= UserCredentials.Empty;

	/// <summary>
	/// The default deadline for calls. Will not be applied to reads or subscriptions.
	/// </summary>
	public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// The retry settings to use for gRPC operations.
	/// </summary>
	public KurrentClientRetrySettings RetrySettings { get; set; } = KurrentClientRetrySettings.NoRetry;

	/// <summary>
	/// Indicates whether the client requires channel-level credentials for secure communication.
	/// </summary>
	public bool RequiresSecureCommunication =>
		ChannelCredentials is not null
	 || DefaultCredentials is not null
	 || ConnectivitySettings.SslCredentials.Required;

	public KurrentClientSchemaSettings SchemaRegistry { get; set; } = KurrentClientSchemaSettings.Default;

	public IMetadataDecoder MetadataDecoder { get; set; } = null!;

	/// <summary>
	/// Creates client settings from a connection string
	/// </summary>
	/// <param name="connectionString">The connection string to parse</param>
	/// <returns>A configured KurrentDBClientSettings instance</returns>
	public static KurrentDBClientSettings Create(string connectionString) =>
		KurrentDBConnectionString.Parse(connectionString).ToClientSettings();
}
