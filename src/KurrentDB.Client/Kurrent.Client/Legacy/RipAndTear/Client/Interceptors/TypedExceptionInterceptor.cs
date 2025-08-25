using Grpc.Core;
using Grpc.Core.Interceptors;

namespace KurrentDB.Client.Interceptors;

class TypedExceptionInterceptor : Interceptor {
	// static readonly Dictionary<string, Func<RpcException, Exception>> DefaultExceptionMap = new() {
	// 	[LegacyErrorCodes.AccessDenied] = ex => new RpcException(new Status(StatusCode.PermissionDenied, ex.Status.Detail, ex.Status.DebugException), ex.Trailers),
	// };

	public TypedExceptionInterceptor(Dictionary<string, Func<RpcException, Exception>> customExceptionMap) {
		//var map = new Dictionary<string, Func<RpcException, Exception>>(customExceptionMap);

		ConvertRpcException = rex => {
			// if (rex.TryMapException(map, out var ex))
			// 	 throw ex;

            if (rex is { StatusCode: StatusCode.Unavailable, Status.Detail: "Deadline Exceeded" })
                throw new RpcException(new Status(StatusCode.DeadlineExceeded, rex.Status.Detail, rex.Status.DebugException), rex.Trailers);

            throw rex;
        };
	}

	Func<RpcException, Exception> ConvertRpcException { get; }

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);

		return new AsyncServerStreamingCall<TResponse>(
			response.ResponseStream.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);

		return new AsyncClientStreamingCall<TRequest, TResponse>(
			response.RequestStream.Apply(ConvertRpcException),
			response.ResponseAsync.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(request, context);

		return new AsyncUnaryCall<TResponse>(
			response.ResponseAsync.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		var response = continuation(context);

		return new AsyncDuplexStreamingCall<TRequest, TResponse>(
			response.RequestStream,
			response.ResponseStream.Apply(ConvertRpcException),
			response.ResponseHeadersAsync,
			response.GetStatus,
			response.GetTrailers,
			response.Dispose
		);
	}
}

static class RpcExceptionConversionExtensions {
	public static IAsyncStreamReader<TRequest> Apply<TRequest>(this IAsyncStreamReader<TRequest> reader, Func<RpcException, Exception> convertException) =>
		new ExceptionConverterStreamReader<TRequest>(reader, convertException);

	public static Task<TResponse> Apply<TResponse>(this Task<TResponse> task, Func<RpcException, Exception> convertException) =>
		task.ContinueWith(t => t.Exception?.InnerException is RpcException ex ? throw convertException(ex) : t.Result);

	public static IClientStreamWriter<TRequest> Apply<TRequest>(
		this IClientStreamWriter<TRequest> writer, Func<RpcException, Exception> convertException
	) => new ExceptionConverterStreamWriter<TRequest>(writer, convertException);

	public static Task Apply(this Task task, Func<RpcException, Exception> convertException) =>
		task.ContinueWith(t => t.Exception?.InnerException is RpcException ex ? throw convertException(ex) : t);

	// public static AccessDeniedException ToAccessDeniedException(this RpcException exception) =>
	// 	new(exception.Message, exception);

	// public static NotLeaderException ToNotLeaderException(this RpcException exception) {
	// 	var host = exception.Trailers.FirstOrDefault(x => x.Key == Exceptions.LeaderEndpointHost)?.Value!;
	// 	var port = exception.Trailers.GetIntValueOrDefault(Exceptions.LeaderEndpointPort);
	// 	return new NotLeaderException(host, port, exception);
	// }

	// public static NotAuthenticatedException ToNotAuthenticatedException(this RpcException exception) =>
	// 	new(exception.Message, exception);

	// public static RpcException ToDeadlineExceededRpcException(this RpcException exception) =>
	// 	new(new Status(StatusCode.DeadlineExceeded, exception.Status.Detail, exception.Status.DebugException));
 //
 //    public static RpcException ToAccessDeniedRpcException(this RpcException exception) =>
 //        new(new Status(StatusCode.PermissionDenied, exception.Status.Detail, exception.Status.DebugException));
 //
 //    const string ExceptionKey = "exception";
 //
	// public static bool TryMapException(this RpcException exception, Dictionary<string, Func<RpcException, Exception>> map, out Exception createdException) {
	// 	if (GrpcMetadataExtensions.TryGetValue(exception.Trailers, ExceptionKey, out var key) && map.TryGetValue(key!, out var factory)) {
	// 		createdException = factory.Invoke(exception);
	// 		return true;
	// 	}
 //
	// 	createdException = null!;
	// 	return false;
	// }
}

class ExceptionConverterStreamReader<TResponse>(IAsyncStreamReader<TResponse> reader, Func<RpcException, Exception> convertException) : IAsyncStreamReader<TResponse> {
	public TResponse Current => reader.Current;

	public async Task<bool> MoveNext(CancellationToken cancellationToken) {
		try {
			return await reader.MoveNext(cancellationToken).ConfigureAwait(false);
		}
		catch (RpcException ex) {
			throw convertException(ex);
		}
	}
}

class ExceptionConverterStreamWriter<TRequest>(IClientStreamWriter<TRequest> writer, Func<RpcException, Exception> convertException)
	: IClientStreamWriter<TRequest> {
	public WriteOptions? WriteOptions {
		get => writer.WriteOptions;
		set => writer.WriteOptions = value;
	}

	public Task WriteAsync(TRequest message) => writer.WriteAsync(message).Apply(convertException);
	public Task CompleteAsync() => writer.CompleteAsync().Apply(convertException);
}
