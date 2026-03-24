// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

using System.Diagnostics;
using Google.Rpc;
using Grpc.Core;
using KurrentDB.Client.Diagnostics;
using KurrentDB.Diagnostics;
using KurrentDB.Diagnostics.Telemetry;
using KurrentDB.Protocol.V2.Streams;
using static KurrentDB.Diagnostics.Tracing.TracingConstants;
using static KurrentDB.Protocol.V2.Streams.StreamsService;
using ConsistencyCheckProto = KurrentDB.Protocol.V2.Streams.ConsistencyCheck;
using Contracts = KurrentDB.Protocol.V2.Streams;

namespace KurrentDB.Client;

public partial class KurrentDBClient {
	/// <summary>
	/// Appends records to one or more streams atomically with cross-stream consistency checks.
	/// Records can be interleaved across streams in any order and the global log preserves
	/// the exact sequence from the request.
	/// </summary>
	/// <param name="records">The records to append. Each record specifies its target stream.</param>
	/// <param name="checks">Optional consistency checks evaluated before commit. If any check fails, the entire transaction is aborted.</param>
	/// <param name="cancellationToken">An optional cancellation token.</param>
	/// <returns>
	/// A task representing the asynchronous operation, with a result of type <see cref="AppendRecordsResponse"/>.
	/// </returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="records"/> is empty.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the server does not support the AppendRecords operation.</exception>
	/// <exception cref="AppendRecordSizeExceededException">Thrown when a single record exceeds the maximum allowed size.</exception>
	/// <exception cref="AppendTransactionMaxSizeExceededException">Thrown when the total transaction size exceeds the maximum allowed size.</exception>
	/// <exception cref="AppendConsistencyViolationException">Thrown when one or more consistency checks fail, including stream revision conflicts and tombstoned stream writes.</exception>
	public async ValueTask<AppendRecordsResponse> AppendRecordsAsync(
		IEnumerable<AppendRecord> records,
		IEnumerable<ConsistencyCheck>? checks = null,
		CancellationToken cancellationToken = default
	) {
		var recordsList = records as IReadOnlyCollection<AppendRecord> ?? records.ToList();

		if (recordsList.Count == 0)
			throw new ArgumentException("At least one record is required.", nameof(records));

		var channelInfo = await GetChannelInfo(cancellationToken).ConfigureAwait(false);

		if (!channelInfo.ServerCapabilities.SupportsAppendRecords)
			throw new InvalidOperationException($"{nameof(AppendRecordsAsync)} requires a server version that supports the AppendRecords operation.");

		var client = new StreamsServiceClient(channelInfo.CallInvoker);

		var tags = new ActivityTagsCollection()
			.WithGrpcChannelServerTags(channelInfo)
			.WithClientSettingsServerTags(Settings)
			.WithOptionalTag(TelemetryTags.Database.User, Settings.DefaultCredentials?.Username);

		return await KurrentDBClientDiagnostics.ActivitySource.TraceClientOperation(Operation, Operations.MultiAppend, tags).ConfigureAwait(false);

		async ValueTask<AppendRecordsResponse> Operation() {
			try {
				var appendRecords = new List<Contracts.AppendRecord>();
				foreach (var record in recordsList) {
					var mapped = await record.Map().ConfigureAwait(false);
					appendRecords.Add(mapped);
				}

				var request = new AppendRecordsRequest {
					Records = { appendRecords }
				};

				if (checks != null) {
					foreach (var check in checks) {
						request.Checks.Add(check.ToProto());
					}
				}

				var response = await client
					.AppendRecordsAsync(request, KurrentDBCallOptions.CreateNonStreaming(Settings, cancellationToken))
					.ResponseAsync
					.ConfigureAwait(false);

				var revisions = response.Revisions
					.Select(r => new AppendResponse(r.Stream, r.Revision))
					.ToList();

				return new AppendRecordsResponse(response.Position, revisions);
			} catch (RpcException ex) {
				var status = ex.GetRpcStatus()!;
				throw status.GetDetail<ErrorInfo>() switch {
					{ Reason: "APPEND_RECORD_SIZE_EXCEEDED" }      => AppendRecordSizeExceededException.FromRpcException(ex),
					{ Reason: "APPEND_TRANSACTION_SIZE_EXCEEDED" } => AppendTransactionMaxSizeExceededException.FromRpcException(ex),
					{ Reason: "APPEND_CONSISTENCY_VIOLATION" }     => AppendConsistencyViolationException.FromRpcException(ex),
					_                                              => ex
				};
			}
		}
	}
}

static class ConsistencyCheckExtensions {
	public static ConsistencyCheckProto ToProto(this ConsistencyCheck check) =>
		check switch {
			ConsistencyCheck.StreamStateCheck s => new ConsistencyCheckProto {
				StreamState = new ConsistencyCheckProto.Types.StreamStateCheck {
					Stream        = s.Stream,
					ExpectedState = s.ExpectedState.ToInt64()
				}
			},
			_ => throw new ArgumentException($"Unknown consistency check type: {check.GetType().Name}", nameof(check))
		};
}
