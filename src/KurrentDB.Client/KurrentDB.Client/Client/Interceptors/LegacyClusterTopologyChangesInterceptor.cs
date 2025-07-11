using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace KurrentDB.Client.Interceptors;

/// <summary>
/// An interceptor that handles detecting connection failures and refreshing cluster info.
/// </summary>
partial class LegacyClusterTopologyChangesInterceptor(LegacyClusterClient clusterClient, ILogger logger) : Interceptor {
	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		var call = continuation(request, context);

		var responseTask = ProcessResponseAsync(call.ResponseAsync);

		return new AsyncUnaryCall<TResponse>(
			responseTask,
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose
		);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var call = continuation(context);

		var responseTask = ProcessResponseAsync(call.ResponseAsync);

		return new AsyncClientStreamingCall<TRequest, TResponse>(
			new ResilientStreamWriter<TRequest>(call.RequestStream, RefreshClusterInfo),
			responseTask,
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose
		);
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var call = continuation(request, context);

		return new AsyncServerStreamingCall<TResponse>(
			new ResilientStreamReader<TResponse>(call.ResponseStream, RefreshClusterInfo),
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose
		);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var call = continuation(context);

		return new AsyncDuplexStreamingCall<TRequest, TResponse>(
			new ResilientStreamWriter<TRequest>(call.RequestStream, RefreshClusterInfo),
			new ResilientStreamReader<TResponse>(call.ResponseStream, RefreshClusterInfo),
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose
		);
	}

	async Task<T> ProcessResponseAsync<T>(Task<T> task) {
		try {
			return await task.ConfigureAwait(false);
		}
		// catch (Exception ex) when (ShouldRefreshClusterInfo(ex, out var leaderEndpoint)) {
		// 	await RefreshClusterInfo(leaderEndpoint);
		// 	throw;
		// }
		catch (Exception ex) {
			await RefreshClusterInfo(ex);
			throw;
		}
	}

	bool ShouldRefreshClusterInfo(Exception ex, out DnsEndPoint? leaderEndpoint) {
		leaderEndpoint = null!;

		if (ex is not RpcException rpcEx) {
			if (ex.InnerException is NotLeaderException lex) {
				leaderEndpoint = lex.LeaderEndpoint;
				LogLeaderChanged(leaderEndpoint.Host, leaderEndpoint.Port);
				return true;
			}

			return false;
		}

		var isLeaderException = rpcEx.Trailers.TryGetValue(Constants.Exceptions.ExceptionKey, out var key)
		                     && key == Constants.Exceptions.NotLeader;

		if (isLeaderException) {
			var host = rpcEx.Trailers.FirstOrDefault(x => x.Key == Constants.Exceptions.LeaderEndpointHost)?.Value!;
			var port = rpcEx.Trailers.GetIntValueOrDefault(Constants.Exceptions.LeaderEndpointPort);
			leaderEndpoint = new DnsEndPoint(host, port);
			LogLeaderChanged(leaderEndpoint.Host, leaderEndpoint.Port);
			return true;
		}

		if (rpcEx.StatusCode == StatusCode.Unavailable) {
			LogUnavailable(rpcEx);
			return true;
		}

		return false;
	}

	async ValueTask RefreshClusterInfo(DnsEndPoint? leaderEndpoint) {
		try {
			await clusterClient
				.ForceReconnect(leaderEndpoint)
				.ConfigureAwait(false);
		}
		catch (Exception refreshEx) {
			LogRefreshFailed(refreshEx);
		}
	}

	async ValueTask RefreshClusterInfo(Exception? ex) {
		try {
			if (ex?.InnerException is NotLeaderException notLeaderEx) {
				LogLeaderChanged(notLeaderEx.LeaderEndpoint.Host, notLeaderEx.LeaderEndpoint.Port);
				await clusterClient.ForceReconnect(notLeaderEx.LeaderEndpoint).ConfigureAwait(false);
			}
			else if (ex is RpcException { StatusCode: StatusCode.Unavailable } rpcEx) {
				LogUnavailable(rpcEx);
				await clusterClient.ForceReconnect().ConfigureAwait(false);
			}
		}
		catch (Exception refreshEx) {
			LogRefreshFailed(refreshEx);
		}
	}

	class ResilientStreamWriter<T>(IClientStreamWriter<T> inner, Func<Exception, ValueTask> refreshClusterInfo) : IClientStreamWriter<T> {
		public WriteOptions? WriteOptions {
			get => inner.WriteOptions;
			set => inner.WriteOptions = value;
		}

		public async Task CompleteAsync() {
			try {
				await inner.CompleteAsync().ConfigureAwait(false);
			}
			catch (Exception ex) {
				await refreshClusterInfo(ex);
				throw;
			}
		}

		public async Task WriteAsync(T message) {
			try {
				await inner.WriteAsync(message).ConfigureAwait(false);
			}
			catch (Exception ex) {
				await refreshClusterInfo(ex);
				throw;
			}
		}
	}

	class ResilientStreamReader<T>(IAsyncStreamReader<T> inner, Func<Exception, ValueTask> refreshClusterInfo) : IAsyncStreamReader<T> {
		public T Current => inner.Current;

		public async Task<bool> MoveNext(CancellationToken cancellationToken) {
			try {
				return await inner.MoveNext(cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex) {
				await refreshClusterInfo(ex);
				throw;
			}
		}
	}

	#region Logging

	[LoggerMessage(Level = LogLevel.Information, Message = "Leader changed to {Host}:{Port}, refreshing cluster info")]
	partial void LogLeaderChanged(string host, int port);

	[LoggerMessage(Level = LogLevel.Information, Message = "Service unavailable, refreshing cluster info")]
	partial void LogUnavailable(RpcException ex);

	[LoggerMessage(Level = LogLevel.Error, Message = "Failed to refresh cluster info")]
	partial void LogRefreshFailed(Exception ex);

	#endregion
}
