using Grpc.Core;
using KurrentDB.Client;
using static EventStore.Client.WrongExpectedVersion.CurrentStreamRevisionOptionOneofCase;
using static EventStore.Client.WrongExpectedVersion.ExpectedStreamPositionOptionOneofCase;

namespace EventStore.Client.Streams;

partial class BatchAppendResp {
	public IWriteResult ToWriteResult() {
		return ResultCase switch {
			ResultOneofCase.Success => new SuccessResult(
				Success.CurrentRevisionOptionCase switch {
					Types.Success.CurrentRevisionOptionOneofCase.CurrentRevision => Success.CurrentRevision,
					_                                                            => StreamState.NoStream
				},
				Success.PositionOptionCase switch {
					Types.Success.PositionOptionOneofCase.Position => new Position(Success.Position.CommitPosition, Success.Position.PreparePosition),
					_                                              => Position.End
				}
			),
			ResultOneofCase.Error => Error.Details switch {
				not null when Error.Details.Is(WrongExpectedVersion.Descriptor)      => FromWrongExpectedVersion(StreamIdentifier, Error.Details.Unpack<WrongExpectedVersion>()),
				not null when Error.Details.Is(StreamDeleted.Descriptor)             => throw new StreamDeletedException(StreamIdentifier!),
				not null when Error.Details.Is(AccessDenied.Descriptor)              => throw new AccessDeniedException(),
				not null when Error.Details.Is(Timeout.Descriptor)                   => throw new RpcException(new Status(StatusCode.DeadlineExceeded, Error.Message)),
				not null when Error.Details.Is(Unknown.Descriptor)                   => throw new InvalidOperationException(Error.Message),
				not null when Error.Details.Is(MaximumAppendSizeExceeded.Descriptor) => throw new MaximumAppendSizeExceededException(Error.Details.Unpack<MaximumAppendSizeExceeded>().MaxAppendSize),
				not null when Error.Details.Is(BadRequest.Descriptor)                => throw new InvalidOperationException(Error.Details.Unpack<BadRequest>().Message),
				_                                                                    => throw new InvalidOperationException($"Could not recognize {Error.Message}")
			},
			_ => throw new InvalidOperationException()
		};
	}

	static WrongExpectedVersionResult FromWrongExpectedVersion(StreamIdentifier streamIdentifier, WrongExpectedVersion wrongExpectedVersion) =>
		new(
			streamIdentifier!,
			wrongExpectedVersion.ExpectedStreamPositionOptionCase switch {
				ExpectedStreamPosition => wrongExpectedVersion.ExpectedStreamPosition,
				_                      => StreamState.Any,
			},
			wrongExpectedVersion.CurrentStreamRevisionOptionCase switch {
				CurrentStreamRevision => wrongExpectedVersion.CurrentStreamRevision,
				_                     => StreamState.NoStream,
			}
		);
}
