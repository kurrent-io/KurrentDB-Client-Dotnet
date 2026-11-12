using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace KurrentDB.Client.Interceptors {
	// this has become more general than just detecting leader changes.
	// triggers the action on any rpc exception with StatusCode.Unavailable or a broken connection
	internal class ReportLeaderInterceptor : Interceptor {
		private readonly Action<ReconnectionRequired> _onReconnectionRequired;

		private const TaskContinuationOptions ContinuationOptions =
			TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted;

		internal ReportLeaderInterceptor(Action<ReconnectionRequired> onReconnectionRequired) {
			_onReconnectionRequired = onReconnectionRequired;
		}

		public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			response.ResponseAsync.ContinueWith(OnReconnectionRequired, ContinuationOptions);

			return new AsyncUnaryCall<TResponse>(response.ResponseAsync, response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			response.ResponseAsync.ContinueWith(OnReconnectionRequired, ContinuationOptions);

			return new AsyncClientStreamingCall<TRequest, TResponse>(
				new StreamWriter<TRequest>(response.RequestStream, OnReconnectionRequired),
				response.ResponseAsync,
				response.ResponseHeadersAsync, response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
			ClientInterceptorContext<TRequest, TResponse> context,
			AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(context);

			return new AsyncDuplexStreamingCall<TRequest, TResponse>(
				new StreamWriter<TRequest>(response.RequestStream, OnReconnectionRequired),
				new StreamReader<TResponse>(response.ResponseStream, OnReconnectionRequired),
				response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
			TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
			AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
			var response = continuation(request, context);

			return new AsyncServerStreamingCall<TResponse>(
				new StreamReader<TResponse>(response.ResponseStream, OnReconnectionRequired),
				response.ResponseHeadersAsync,
				response.GetStatus, response.GetTrailers, response.Dispose);
		}

		private void OnReconnectionRequired(Task task) {
			var reconnectionRequired = GetReconnectionRequired(task.Exception?.InnerException);

			if (reconnectionRequired is not ReconnectionRequired.None)
				_onReconnectionRequired(reconnectionRequired);
		}

		internal static ReconnectionRequired GetReconnectionRequired(Exception? exception) =>
			exception switch {
				NotLeaderException ex => new ReconnectionRequired.NewLeader(ex.LeaderEndpoint),
				RpcException {
					StatusCode: StatusCode.Unavailable
					// or StatusCode.Unknown or TODO: use RPC exceptions on server
				} => ReconnectionRequired.Rediscover.Instance,
				RpcException ex when IsBrokenConnection(ex) => ReconnectionRequired.Rediscover.Instance,
				_ => ReconnectionRequired.None.Instance
			};

		// The HTTP/2 connection failed, e.g. because the node stopped answering keepalive pings after it froze or
		// the network to it failed. Grpc.Net.Client reports these as StatusCode.Internal rather than Unavailable,
		// and the node may no longer be the leader, so look for the leader again instead of redialling it.
		private static bool IsBrokenConnection(RpcException exception) {
#if NET
			for (var current = exception.Status.DebugException; current is not null; current = current.InnerException) {
				if (current is HttpProtocolException)
					return true;
			}
#endif
			return false;
		}

		private class StreamWriter<T> : IClientStreamWriter<T> {
			private readonly IClientStreamWriter<T> _inner;
			private readonly Action<Task> _reportNewLeader;

			public StreamWriter(IClientStreamWriter<T> inner, Action<Task> reportNewLeader) {
				_inner = inner;
				_reportNewLeader = reportNewLeader;
			}

			public WriteOptions? WriteOptions {
				get => _inner.WriteOptions;
				set => _inner.WriteOptions = value;
			}

			public Task CompleteAsync() {
				var task = _inner.CompleteAsync();
				task.ContinueWith(_reportNewLeader, ContinuationOptions);
				return task;
			}

			public Task WriteAsync(T message) {
				var task = _inner.WriteAsync(message);
				task.ContinueWith(_reportNewLeader, ContinuationOptions);
				return task;
			}
		}

		private class StreamReader<T> : IAsyncStreamReader<T> {
			private readonly IAsyncStreamReader<T> _inner;
			private readonly Action<Task> _reportNewLeader;

			public StreamReader(IAsyncStreamReader<T> inner, Action<Task> reportNewLeader) {
				_inner = inner;
				_reportNewLeader = reportNewLeader;
			}

			public Task<bool> MoveNext(CancellationToken cancellationToken) {
				var task = _inner.MoveNext(cancellationToken);
				task.ContinueWith(_reportNewLeader, ContinuationOptions);
				return task;
			}

			public T Current => _inner.Current;
		}
	}
}
