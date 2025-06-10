using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Kurrent.Grpc.Compression;

/// <summary>
/// A gRPC interceptor that applies compression to outgoing requests using the specified compression provider.
/// </summary>
public class CompressionInterceptor : Interceptor {
	const string CompressionRequestAlgorithmHeader = "grpc-internal-encoding-request";

	/// <summary>
	/// Initializes a new instance of the <see cref="CompressionInterceptor"/> class.
	/// </summary>
	/// <param name="compressionMethod">The name of the compression method to use.</param>
	public CompressionInterceptor(string compressionMethod) {
		if (string.IsNullOrEmpty(compressionMethod))
			throw new ArgumentException("Compression method cannot be null or empty", nameof(compressionMethod));

		CompressionMethod = compressionMethod;
	}

	string CompressionMethod { get; }

	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation
	) {
		ApplyCompression(context);
		return continuation(request, context);
	}

	public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		ApplyCompression(context);
		return continuation(context);
	}

	public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		ApplyCompression(context);
		return continuation(request, context);
	}

	public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation
	) {
		ApplyCompression(context);
		return continuation(context);
	}

	void ApplyCompression<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
		where TRequest : class where TResponse : class {
		if (context.Options.Headers is null)
			return;

		var hasCompressionHeader = context.Options.Headers.Any(m =>
			string.Equals(m.Key, CompressionRequestAlgorithmHeader, StringComparison.OrdinalIgnoreCase)
		);

		if (!hasCompressionHeader)
			context.Options.Headers.Add(CompressionRequestAlgorithmHeader, CompressionMethod);
	}
}
