using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KurrentDb.Client;

/// <summary>
/// The client used to perform maintenance and other administrative tasks on the KurrentDB.
/// </summary>
public sealed partial class KurrentDbOperationsClient : KurrentDbClientBase {
	static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap =
		new() {
			[Constants.Exceptions.ScavengeNotFound] = ex => new ScavengeNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.ScavengeId)?.Value
			)
		};

	readonly ILogger _log;

	/// <summary>
	/// Constructs a new <see cref="KurrentDbOperationsClient"/>. This method is not intended to be called directly in your code.
	/// </summary>
	/// <param name="options"></param>
	public KurrentDbOperationsClient(IOptions<KurrentDbClientSettings> options) : this(options.Value) { }

	/// <summary>
	/// Constructs a new <see cref="KurrentDbOperationsClient"/>.
	/// </summary>
	/// <param name="settings"></param>
	public KurrentDbOperationsClient(KurrentDbClientSettings? settings = null) : base(settings, ExceptionMap) =>
		_log = Settings.LoggerFactory?.CreateLogger<KurrentDbOperationsClient>() ?? new NullLogger<KurrentDbOperationsClient>();
}
