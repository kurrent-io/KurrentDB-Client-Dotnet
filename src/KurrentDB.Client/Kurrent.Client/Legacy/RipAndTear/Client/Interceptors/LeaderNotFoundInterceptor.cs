using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Kurrent.Client.Legacy;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace KurrentDB.Client.Interceptors;

/// <summary>
/// An interceptor that detects when the leader node changes or when the service is unavailable
/// and triggers a cluster topology refresh.
/// </summary>
partial class LeaderNotFoundInterceptor : Interceptor {
	const TaskContinuationOptions ContinuationOptions = ExecuteSynchronously | OnlyOnFaulted;

    const string ErrorCode       = "not-leader";
    const string EndpointHostKey = "leader-endpoint-host";
    const string EndpointPortKey = "leader-endpoint-port";

    public LeaderNotFoundInterceptor(LegacyClusterClient clusterClient, ILogger logger) {
        Logger = logger;

        CheckForLeaderChange = task => {
            if (task.Exception?.InnerException is not RpcException rex || !rex.IsLegacyError(ErrorCode))
                return;

            var host = rex.Trailers.GetValue(EndpointHostKey)!;
            var port = int.Parse(rex.Trailers.GetValue(EndpointPortKey)!);

            var newLeaderEndpoint = new DnsEndPoint(host, port);

            LogLeaderChanged(Logger, newLeaderEndpoint.Host, newLeaderEndpoint.Port);

            clusterClient.TriggerReconnect(newLeaderEndpoint);
        };
    }

	ILogger      Logger               { get; }
	Action<Task> CheckForLeaderChange { get; }

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);
		response.ResponseAsync.ContinueWith(CheckForLeaderChange, ContinuationOptions);
		return new(
			response.ResponseAsync, response.ResponseHeadersAsync, response.GetStatus,
			response.GetTrailers, response.Dispose
		);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);
		response.ResponseAsync.ContinueWith(CheckForLeaderChange, ContinuationOptions);
		return new(
			new StreamWriter<TRequest>(response.RequestStream, CheckForLeaderChange),
			response.ResponseAsync, response.ResponseHeadersAsync,
			response.GetStatus, response.GetTrailers, response.Dispose
		);
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);
		return new(
			new StreamReader<TResponse>(response.ResponseStream, CheckForLeaderChange),
			response.ResponseHeadersAsync, response.GetStatus,
			response.GetTrailers, response.Dispose
		);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);
		return new(
			new StreamWriter<TRequest>(response.RequestStream, CheckForLeaderChange),
			new StreamReader<TResponse>(response.ResponseStream, CheckForLeaderChange), response.ResponseHeadersAsync, response.GetStatus,
			response.GetTrailers, response.Dispose
		);
	}

	class StreamWriter<T>(IClientStreamWriter<T> inner, Action<Task> onTaskFaulted) : IClientStreamWriter<T> {
		public WriteOptions? WriteOptions {
			get => inner.WriteOptions;
			set => inner.WriteOptions = value;
		}

		public Task CompleteAsync() {
			var task = inner.CompleteAsync();
			task.ContinueWith(onTaskFaulted, ContinuationOptions);
			return task;
		}

		public Task WriteAsync(T message) {
			var task = inner.WriteAsync(message);
			task.ContinueWith(onTaskFaulted, ContinuationOptions);
			return task;
		}
	}

	class StreamReader<T>(IAsyncStreamReader<T> inner, Action<Task> onTaskFaulted) : IAsyncStreamReader<T> {
		public T Current => inner.Current;
		public Task<bool> MoveNext(CancellationToken cancellationToken) {
			var task = inner.MoveNext(cancellationToken);
			task.ContinueWith(onTaskFaulted, ContinuationOptions);
			return task;
		}
	}

	#region Logging

	[LoggerMessage(Level = LogLevel.Warning, Message = "Leader changed to {Host}:{Port}, refreshing cluster info...")]
	static partial void LogLeaderChanged(ILogger logger, string host, int port);

	[LoggerMessage(Level = LogLevel.Debug, Message = "Service unavailable, refreshing cluster info...")]
	static partial void LogUnavailable(ILogger logger, RpcException ex);

	#endregion
}
