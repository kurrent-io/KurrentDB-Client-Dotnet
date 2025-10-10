// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable PossibleMultipleEnumeration

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using Grpc.Core;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Protocol.V2.Streams;
using static KurrentDB.Diagnostics.Tracing.TracingConstants;
using static KurrentDB.Protocol.V2.Streams.StreamsService;

namespace KurrentDB.Client;

public partial class KurrentDBClient {
	/// <summary>
	/// Appends events to multiple streams asynchronously, ensuring the specified state of each stream is respected.
	/// </summary>
	/// <param name="requests">An asynchronous enumerable of <see cref="AppendStreamRequest"/> objects, each containing details of the stream, expected stream state, and events to append.</param>
	/// <param name="cancellationToken">An optional cancellation token to observe while waiting for the operation to complete.</param>
	/// <returns>
	/// A task that represents the asynchronous operation, with a result of type <see cref="MultiStreamAppendResponse"/>, indicating the outcome of the operation.
	/// <para>
	/// On success, returns <see cref="MultiStreamAppendResponse"/> containing the successful append results.
	/// <see cref="WrongExpectedVersionException"/>, <see cref="AccessDeniedException"/>, <see cref="StreamDeletedException"/>,  or <see cref="TransactionMaxSizeExceededException"/>.
	/// </para>
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if the server does not support multi-stream append functionality (requires server version 25.1 or higher).</exception>
	public async ValueTask<MultiStreamAppendResponse> MultiStreamAppendAsync(
		IAsyncEnumerable<AppendStreamRequest> requests, CancellationToken cancellationToken = default
	) {
		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

		if (!channelInfo.ServerCapabilities.SupportsMultiStreamAppend)
			throw new InvalidOperationException($"{nameof(MultiStreamAppendAsync)} requires server version 25.1 or higher.");

		var client = new StreamsServiceClient(channelInfo.CallInvoker);

		var tags = new ActivityTagsCollection()
			.WithGrpcChannelServerTags(channelInfo)
			.WithClientSettingsServerTags(Settings)
			.WithOptionalTag(TelemetryTags.Database.User, Settings.DefaultCredentials?.Username);

		return await KurrentDBClientDiagnostics.ActivitySource.TraceClientOperation(Operation, Operations.MultiAppend, tags).ConfigureAwait(false);

		async ValueTask<MultiStreamAppendResponse> Operation() {
			try {
				using var session = client.AppendSession(KurrentDBCallOptions.CreateStreaming(Settings, cancellationToken: cancellationToken));

				await foreach (var request in requests.WithCancellation(cancellationToken)) {
					var records = await request.Messages
						.Map()
						.ToArrayAsync(cancellationToken)
						.ConfigureAwait(false);

					var serviceRequest = new AppendRequest {
						Stream           = request.Stream,
						ExpectedRevision = request.ExpectedState.ToInt64(),
						Records          = { records }
					};

					// Cancellation of stream writes is not supported by this gRPC implementation.
					// To cancel the operation, we should cancel the entire session.
					await session.RequestStream
						.WriteAsync(serviceRequest)
						.ConfigureAwait(false);
				}

				await session.RequestStream.CompleteAsync();

				var response = await session.ResponseAsync;

				var responses = response.Output.Select(appendResponse => new AppendResponse(appendResponse.Stream, appendResponse.StreamRevision));

				return new MultiStreamAppendResponse(response.Position, responses);
			} catch (RpcException ex) {
				throw ex.MapRpcException();
			}
		}
	}
}
