using System.Diagnostics.CodeAnalysis;
using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using static System.Threading.Tasks.TaskContinuationOptions;

namespace KurrentDB.Client.Interceptors;

/// <summary>
/// An interceptor that detects when the leader node changes or when the service is unavailable
/// and triggers a cluster topology refresh.
/// </summary>
partial class ClusterTopologyChangesInterceptor : Interceptor {
	const TaskContinuationOptions ContinuationOptions = ExecuteSynchronously | OnlyOnFaulted;

	public ClusterTopologyChangesInterceptor(LegacyClusterClient clusterClient, ILogger logger) {
		Logger = logger;

		CheckForLeaderChange = task => {
			switch (task.Exception?.InnerException) {
				case RpcException rex when IsNotLeaderException(rex, out var newLeaderEndpoint):
					LogLeaderChanged(Logger, newLeaderEndpoint.Host, newLeaderEndpoint.Port);
					clusterClient.TriggerReconnect(newLeaderEndpoint);
					break;
			}
		};

		return;

		static bool IsNotLeaderException(RpcException rex, [MaybeNullWhen(false)] out DnsEndPoint endpoint) {
			const string exceptionKey           = "exception";
			const string notLeaderExceptionType = "not-leader";
			const string leaderEndpointHostKey  = "leader-endpoint-host";
			const string leaderEndpointPortKey  = "leader-endpoint-port";

			if (!rex.Trailers.TryGetValue(exceptionKey, out var value) || value != notLeaderExceptionType) {
				endpoint = null;
				return false;
			}

			endpoint = new(
				rex.Trailers.GetValue(leaderEndpointHostKey)!,
				int.Parse(rex.Trailers.GetValue(leaderEndpointPortKey)!));
			return true;
		}
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

		public Task CompleteAsync()       => inner.CompleteAsync().ContinueWith(onTaskFaulted, ContinuationOptions);
		public Task WriteAsync(T message) => inner.WriteAsync(message).ContinueWith(onTaskFaulted, ContinuationOptions);
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
