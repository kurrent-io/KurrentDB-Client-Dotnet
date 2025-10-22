using Grpc.Core;
using KurrentDB.Protocol.V2.Streams.Errors;

namespace KurrentDB.Client;

/// <summary>
/// Exception thrown when a transaction exceeds the allowed maximum size limit.
/// </summary>
public class AppendTransactionMaxSizeExceededException(int size, int maxSize, Exception? innerException = null)
	: Exception(
		$"The total size of the append transaction ({size}) exceeds the maximum allowed size of {maxSize} bytes by {size - maxSize}",
		innerException
	) {
	/// <summary>
	/// The size of the huge transaction in bytes.
	/// </summary>
	public int Size { get; } = size;

	/// <summary>
	/// The maximum allowed size of the append transaction in bytes.
	/// </summary>
	public int MaxSize { get; } = maxSize;

	public static AppendTransactionMaxSizeExceededException FromRpcException(RpcException ex) => FromRpcStatus(ex.GetRpcStatus()!);

	public static AppendTransactionMaxSizeExceededException FromRpcStatus(Google.Rpc.Status ex) {
		var details = ex.GetDetail<AppendTransactionSizeExceededErrorDetails>();
		return new AppendTransactionMaxSizeExceededException(
			details.Size,
			details.MaxSize
		);
	}
}
