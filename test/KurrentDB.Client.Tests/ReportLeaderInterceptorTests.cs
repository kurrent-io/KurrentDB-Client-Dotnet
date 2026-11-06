using System.Net;
using System.Net.Http;
using Grpc.Core;
using Grpc.Core.Interceptors;
using KurrentDB.Client.Interceptors;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class ReportLeaderInterceptorTests {
	[Fact]
	public void NotLeaderReconnectsToTheNewLeader() =>
		ReportLeaderInterceptor.GetReconnectionRequired(new NotLeaderException("node2", 2113))
			.ShouldBe(new ReconnectionRequired.NewLeader(new DnsEndPoint("node2", 2113)));

	[Fact]
	public void UnavailableRediscovers() =>
		ReportLeaderInterceptor.GetReconnectionRequired(new RpcException(new Status(StatusCode.Unavailable, "")))
			.ShouldBe(ReconnectionRequired.Rediscover.Instance);

	[Theory]
	[InlineData(StatusCode.Internal)]
	[InlineData(StatusCode.DeadlineExceeded)]
	[InlineData(StatusCode.Cancelled)]
	[InlineData(StatusCode.NotFound)]
	public void OtherStatusesDoNotReconnect(StatusCode statusCode) =>
		ReportLeaderInterceptor.GetReconnectionRequired(new RpcException(new Status(statusCode, "")))
			.ShouldBe(ReconnectionRequired.None.Instance);

	[Fact]
	public void InternalWithoutAProtocolErrorDoesNotReconnect() =>
		ReportLeaderInterceptor.GetReconnectionRequired(
				new RpcException(new Status(StatusCode.Internal, "Error starting gRPC call.", new HttpRequestException("Some other failure.")))
			)
			.ShouldBe(ReconnectionRequired.None.Instance);

#if NET
	// What Grpc.Net.Client reports when the node stops answering keepalive pings, e.g. because it froze or the
	// network to it failed
	static RpcException KeepAlivePingTimeout() =>
		new(
			new Status(
				StatusCode.Internal,
				"Error starting gRPC call. HttpRequestException: The HTTP/2 server didn't respond to a ping request within the configured KeepAlivePingDelay.",
				new HttpRequestException(
					HttpRequestError.HttpProtocolError,
					"The HTTP/2 server didn't respond to a ping request within the configured KeepAlivePingDelay.",
					new HttpProtocolException(0, "The HTTP/2 server didn't respond to a ping request within the configured KeepAlivePingDelay.", null)
				)
			)
		);

	[Fact]
	public void BrokenConnectionRediscovers() =>
		ReportLeaderInterceptor.GetReconnectionRequired(KeepAlivePingTimeout())
			.ShouldBe(ReconnectionRequired.Rediscover.Instance);

	[Fact]
	public void ProtocolErrorOnTheCallRediscovers() =>
		ReportLeaderInterceptor.GetReconnectionRequired(
				new RpcException(new Status(StatusCode.Internal, "", new HttpProtocolException(1, "The HTTP/2 server reset the stream.", null)))
			)
			.ShouldBe(ReconnectionRequired.Rediscover.Instance);

	[Fact]
	public async Task FailedCallReportsBrokenConnection() {
		var reported = new List<ReconnectionRequired>();
		var sut      = new ReportLeaderInterceptor(reported.Add);
		var method = new Method<string, string>(
			MethodType.Unary, "service", "method", Marshallers.StringMarshaller, Marshallers.StringMarshaller
		);

		var call = sut.AsyncUnaryCall(
			"request",
			new ClientInterceptorContext<string, string>(method, null, new CallOptions()),
			(_, _) => new AsyncUnaryCall<string>(
				Task.FromException<string>(KeepAlivePingTimeout()),
				Task.FromResult(new Metadata()),
				() => Status.DefaultSuccess,
				() => new Metadata(),
				() => { }
			)
		);

		await Should.ThrowAsync<RpcException>(call.ResponseAsync);
		reported.ShouldBe([ReconnectionRequired.Rediscover.Instance]);
	}
#endif
}
