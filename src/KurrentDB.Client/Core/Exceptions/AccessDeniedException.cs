using Grpc.Core;

namespace KurrentDB.Client {
	/// <summary>
	/// Exception thrown when a user is not authorised to carry out
	/// an operation.
	/// </summary>
	public class AccessDeniedException : Exception {
		/// <summary>
		/// Constructs a new <see cref="AccessDeniedException" />.
		/// </summary>
		public AccessDeniedException(string message, Exception innerException) : base(message, innerException) {
		}

		/// <summary>
		/// Constructs a new <see cref="AccessDeniedException" />.
		/// </summary>
		public AccessDeniedException() : base("Access denied.") {

		}

		public static AccessDeniedException FromRpcException(RpcException ex) => FromRpcStatus(ex.GetRpcStatus()!);

		public static AccessDeniedException FromRpcStatus(Google.Rpc.Status ex) {
			return new(ex.Message, ex.ToRpcException());
		}
	}
}
