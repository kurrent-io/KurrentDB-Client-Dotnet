// ReSharper disable InconsistentNaming

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace KurrentDB.Client.Interceptors;

/// <summary>
/// Interceptor that adds authentication headers to gRPC calls.
/// </summary>
class AuthenticationInterceptor(KurrentDBClientSettings Settings) : Interceptor {
    /// <summary>
    /// Adds authentication headers to unary calls.
    /// </summary>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation) {
        var modifiedContext = AddAuthHeaderToContext(context);
        return continuation(request, modifiedContext);
    }

    /// <summary>
    /// Adds authentication headers to client streaming calls.
    /// </summary>
    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation) {
        var modifiedContext = AddAuthHeaderToContext(context);
        return continuation(modifiedContext);
    }

    /// <summary>
    /// Adds authentication headers to server streaming calls.
    /// </summary>
    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation) {
        var modifiedContext = AddAuthHeaderToContext(context);
        return continuation(request, modifiedContext);
    }

    /// <summary>
    /// Adds authentication headers to duplex streaming calls.
    /// </summary>
    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation) {
        var modifiedContext = AddAuthHeaderToContext(context);
        return continuation(modifiedContext);
    }

    ClientInterceptorContext<TRequest, TResponse> AddAuthHeaderToContext<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class {

        if (Settings.DefaultCredentials is null)
	        return context;

        var headers = context.Options.Headers ?? [];

        if (context.Options.Headers is null) {
            var callOptions = context.Options.WithHeaders(headers);
            context = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, callOptions);
        }

        var authHeader = Settings.OperationOptions
            .GetAuthenticationHeaderValue(Settings.DefaultCredentials, context.Options.CancellationToken)
            .AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

        headers.Add(Constants.Headers.Authorization, authHeader);

        return context;
    }
}
