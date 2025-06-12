using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Kurrent.Client.Grpc.Interceptors;

/// <summary>
/// Interceptor that adds headers to gRPC calls based on specified operations.
/// </summary>
class HeadersInterceptor : Interceptor {
    readonly Dictionary<string, string> _headers;
    readonly string[]                   _operations;
    readonly bool                       _isExcludeList;

    HeadersInterceptor(Dictionary<string, string> headers, string[] operations, bool isExcludeList) {
        _headers       = headers;
        _operations    = operations;
        _isExcludeList = isExcludeList;
    }

    public static HeadersInterceptor InjectHeaders(Dictionary<string, string> headers, params string[] operationToExclude) {
        ArgumentOutOfRangeException.ThrowIfEqual(headers.Count, 0);

        foreach (var header in headers)
            ArgumentException.ThrowIfNullOrWhiteSpace(header.Key);

        foreach (var operation in operationToExclude)
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        return new(headers, operationToExclude, true);
    }

    public static HeadersInterceptor InjectHeaders(string key, string value, params string[] operationsToExclude) {
        foreach (var operation in operationsToExclude)
            ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        return InjectHeaders(new() { [key] = value }, operationsToExclude);
    }

    public static HeadersInterceptor InjectHeaders(Dictionary<string, string> headers) =>
        new(headers, [], false);

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

    void AddHeaders<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context) where TRequest : class where TResponse : class {
        if (context.Options.Headers is null)
            return;

        // If no operations are specified, or the current operation is not in the list, skip adding headers
        // If ignore is true, skip adding headers regardless of the operation
        var skipOperation = _operations.Length > 0
                         && !_operations.Contains(context.Method.Name, StringComparer.OrdinalIgnoreCase)
                         || _isExcludeList;

        if (skipOperation) return;

        foreach (var header in _headers)
            context.Options.Headers.Add(header.Key, header.Value);
    }
}
