using System.Net;
using Grpc.Core;
using Kurrent.Rpc;

namespace KurrentDB.Client {
	/// <summary>
	/// The exception that is thrown when an operation requiring a leader node is made on a follower node.
	/// </summary>
	public class NotLeaderException : Exception {

		/// <summary>
		/// The <see cref="EndPoint"/> of the current leader node.
		/// </summary>
		public DnsEndPoint LeaderEndpoint { get; }

		/// <summary>
		/// Constructs a new <see cref="NotLeaderException"/>
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="exception"></param>
		public NotLeaderException(string host, int port, Exception? exception = null) : base(
			$"Not leader. New leader at {host}:{port}.", exception) {
			LeaderEndpoint = new DnsEndPoint(host, port);
		}

		public static NotLeaderException FromRpcException(RpcException ex) => FromRpcStatus(ex.GetRpcStatus()!);

		public static NotLeaderException FromRpcStatus(Google.Rpc.Status ex) {
			var details = ex.GetDetail<NotLeaderNodeErrorDetails>();
			return new NotLeaderException(
				details.CurrentLeader.Host,
				details.CurrentLeader.Port
			);
		}
	}
}
