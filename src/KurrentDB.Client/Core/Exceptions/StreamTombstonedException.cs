using Grpc.Core;
using KurrentDB.Protocol.V2.Streams.Errors;

namespace KurrentDB.Client;

public class StreamTombstonedException : Exception {
	/// <summary>
	/// The name of the tombstoned stream.
	/// </summary>
	public readonly string Stream;

	/// <summary>
	/// Constructs a new instance of <see cref="StreamTombstonedException"/>.
	/// </summary>
	/// <param name="stream">The name of the tombstoned stream.</param>
	/// <param name="exception"></param>
	public StreamTombstonedException(string stream, Exception? exception = null)
		: base($"Event stream '{stream}' is tombstoned.", exception) {
		Stream = stream;
	}

	public static StreamTombstonedException FromRpcException(RpcException ex) => FromRpcStatus(ex.GetRpcStatus()!);

	public static StreamTombstonedException FromRpcStatus(Google.Rpc.Status ex) {
		var details = ex.GetDetail<StreamTombstonedErrorDetails>();
		return new StreamTombstonedException(details.Stream);
	}
}
