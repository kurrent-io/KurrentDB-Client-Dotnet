using EventStore.Client;
using Grpc.Core;
using KurrentDB.Client;
using static EventStore.Client.WrongExpectedVersion.CurrentStreamRevisionOptionOneofCase;
using static EventStore.Client.WrongExpectedVersion.ExpectedStreamPositionOptionOneofCase;
using Position = KurrentDB.Client.Position;
using Timeout = EventStore.Client.Timeout;

namespace KurrentDB.Protocol.Streams.V1 {
	partial class BatchAppendResp {
		public IWriteResult ToWriteResult() => ResultCase switch {
			ResultOneofCase.Success => new SuccessResult(
				Success.CurrentRevisionOptionCase switch {
					Types.Success.CurrentRevisionOptionOneofCase.CurrentRevision => Success.CurrentRevision,
					_                                                            => StreamState.NoStream
				}, Success.PositionOptionCase switch {
					Types.Success.PositionOptionOneofCase.Position => new Position(
						Success.Position.CommitPosition,
						Success.Position.PreparePosition),
					_ => Position.End
				}),
			ResultOneofCase.Error => Error.Details.FirstOrDefault() switch {
				{ } detail when detail.Is(WrongExpectedVersion.Descriptor) =>
					FromWrongExpectedVersion(StreamIdentifier, detail.Unpack<WrongExpectedVersion>()),
				{ } detail when detail.Is(StreamDeleted.Descriptor) =>
					throw new StreamDeletedException(StreamIdentifier!),
				{ } detail when detail.Is(AccessDenied.Descriptor) => throw new AccessDeniedException(),
				{ } detail when detail.Is(Timeout.Descriptor) => throw new RpcException(
					new Status(StatusCode.DeadlineExceeded, Error.Message)),
				{ } detail when detail.Is(Unknown.Descriptor) => throw new InvalidOperationException(Error.Message),
				{ } detail when detail.Is(MaximumAppendSizeExceeded.Descriptor) =>
					throw new MaximumAppendSizeExceededException(
						detail.Unpack<MaximumAppendSizeExceeded>().MaxAppendSize),
				{ } detail when detail.Is(BadRequest.Descriptor) => throw new InvalidOperationException(detail
					.Unpack<BadRequest>().Message),
				_ => throw new InvalidOperationException($"Could not recognize {Error.Message}")
			},
			_ => throw new InvalidOperationException()
		};

		private static WrongExpectedVersionResult FromWrongExpectedVersion(StreamIdentifier streamIdentifier,
			WrongExpectedVersion wrongExpectedVersion) => new(streamIdentifier!,
			wrongExpectedVersion.ExpectedStreamPositionOptionCase switch {
				ExpectedStreamPosition => wrongExpectedVersion.ExpectedStreamPosition,
				_ => StreamState.Any,
			}, wrongExpectedVersion.CurrentStreamRevisionOptionCase switch {
				CurrentStreamRevision => wrongExpectedVersion.CurrentStreamRevision,
				_ => StreamState.NoStream,
			});
	}
}
