using Grpc.Core;
using Microsoft.Extensions.Options;

namespace KurrentDB.Client;

/// <summary>
/// The client used to perform maintenance and other administrative tasks on the KurrentDB.
/// </summary>
public sealed partial class KurrentDBOperationsClient : KurrentDBClientBase {
	static readonly Dictionary<string, Func<RpcException, Exception>> ExceptionMap =
		new() {
			[Constants.Exceptions.ScavengeNotFound] = ex => new ScavengeNotFoundException(
				ex.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.ScavengeId)?.Value
			)
		};


	/// <summary>
	/// Constructs a new <see cref="KurrentDBOperationsClient"/>. This method is not intended to be called directly in your code.
	/// </summary>
	/// <param name="options"></param>
	public KurrentDBOperationsClient(IOptions<KurrentDBClientSettings> options) : this(options.Value) { }

	/// <summary>
	/// Constructs a new <see cref="KurrentDBOperationsClient"/>.
	/// </summary>
	/// <param name="settings"></param>
	public KurrentDBOperationsClient(KurrentDBClientSettings? settings = null) : base(settings, ExceptionMap) { }
}
