// ReSharper disable InconsistentNaming

using Grpc.Core;
using Grpc.Core.Interceptors;
using static System.StringComparer;

namespace KurrentDB.Client.Interceptors;

sealed class RequiresLeaderInterceptor(params string[]? excludedOperations) : Interceptor {
	readonly HashSet<string> ExcludedOperations = new(excludedOperations ?? [], OrdinalIgnoreCase);

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) where TRequest : class where TResponse : class =>
		continuation(request, PrepareContext(context));

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) where TRequest : class where TResponse : class =>
		continuation(PrepareContext(context));

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) where TRequest : class where TResponse : class =>
		continuation(request, PrepareContext(context));

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) where TRequest : class where TResponse : class =>
		continuation(PrepareContext(context));

	ClientInterceptorContext<TRequest, TResponse> PrepareContext<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context
	) where TRequest : class where TResponse : class {
		var operationName = Path.GetFileName(context.Method.Name);

		return ExcludedOperations.Contains(operationName)
			? RemoveRequiresLeaderHeader(context)
			: context;
	}

	static ClientInterceptorContext<TRequest, TResponse> RemoveRequiresLeaderHeader<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context
	) where TRequest : class where TResponse : class {
		if (context.Options.Headers is null)
			return context;

		var headers = new Metadata();

		context.Options.Headers
			.Where(header => header.Key is not Constants.Headers.RequiresLeader)
			.ToList()
			.ForEach(headers.Add);

		return new ClientInterceptorContext<TRequest, TResponse>(
			context.Method,
			context.Host,
			context.Options.WithHeaders(headers)
		);
	}
}
