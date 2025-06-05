using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Kurrent.Client.Infra.Interceptors;

/// <summary>
/// A generic gRPC interceptor that adds custom headers to all requests
/// </summary>
class HeadersInterceptor(Dictionary<string, string> headers) : Interceptor {
	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		AddHeaders(context);
		return continuation(request, context);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		AddHeaders(context);
		return continuation(context);
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		AddHeaders(context);
		return continuation(request, context);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		AddHeaders(context);
		return continuation(context);
	}

	void AddHeaders<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
		where TRequest : class where TResponse : class {
		if (context.Options.Headers is null)
			return;

		foreach (var header in headers)
			context.Options.Headers.Add(header.Key, header.Value);
	}
}
