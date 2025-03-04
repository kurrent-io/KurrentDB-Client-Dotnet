using System;
using System.Collections.Generic;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KurrentDb.Client {
	/// <summary>
	///The client used to manage projections on the KurrentDB.
	/// </summary>
	public sealed partial class KurrentDBProjectionManagementClient : KurrentDbClientBase {
		private readonly ILogger _log;

		/// <summary>
		/// Constructs a new <see cref="KurrentDBProjectionManagementClient"/>. This method is not intended to be called directly from your code.
		/// </summary>
		/// <param name="options"></param>
		public KurrentDBProjectionManagementClient(IOptions<KurrentDbClientSettings> options) : this(options.Value) {
		}

		/// <summary>
		/// Constructs a new <see cref="KurrentDBProjectionManagementClient"/>.
		/// </summary>
		/// <param name="settings"></param>
		public KurrentDBProjectionManagementClient(KurrentDbClientSettings? settings) : base(settings,
			new Dictionary<string, Func<RpcException, Exception>>()) {
			_log = settings?.LoggerFactory?.CreateLogger<KurrentDBProjectionManagementClient>() ??
			       new NullLogger<KurrentDBProjectionManagementClient>();
		}
	}
}
