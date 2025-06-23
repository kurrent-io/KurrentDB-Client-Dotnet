using Kurrent.Client.Model;
using KurrentDB.Client;

namespace Kurrent.Client.Legacy;

#pragma warning disable CS8509
static class KurrentDBLegacyExceptionMappers {
    public static ErrorDetails.StreamNotFound AsStreamNotFoundError(this Exception ex) =>
        ex is StreamNotFoundException lex
            ? new(x => x.With("stream", lex.Stream))
            : throw new InvalidCastException($"Expected {nameof(StreamNotFoundException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.StreamRevisionConflict)}.", ex);

    public static ErrorDetails.StreamDeleted AsStreamDeletedError(this Exception ex) =>
        ex is StreamDeletedException lex
            ? new(x => x.With("stream", lex.Stream))
            : throw new InvalidCastException($"Expected {nameof(StreamDeletedException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.StreamRevisionConflict)}.", ex);

    public static ErrorDetails.AccessDenied AsAccessDeniedError(this Exception ex, StreamName stream) =>
        ex is AccessDeniedException
            ? new(x => x.With("stream", stream))
            : throw new InvalidCastException($"Expected {nameof(AccessDeniedException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.StreamRevisionConflict)}.", ex);

    public static ErrorDetails.StreamRevisionConflict AsStreamRevisionConflict(this Exception ex) =>
        ex is WrongExpectedVersionException lex
            ? new(x => x.With("stream", lex.StreamName).With("expected-revision", lex.ActualVersion))
            : throw new InvalidCastException($"Expected {nameof(WrongExpectedVersionException)} but got {ex.GetType().Name} while mapping to {nameof(ErrorDetails.StreamRevisionConflict)}.", ex);

    public static StreamState MapToLegacyExpectedState(this ExpectedStreamState expectedState) {
        return expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => StreamState.Any,
            _ when expectedState == ExpectedStreamState.NoStream     => StreamState.NoStream,
            _ when expectedState == ExpectedStreamState.StreamExists => StreamState.StreamExists,
            _                                                        => StreamState.StreamRevision(expectedState)
        };
    }
}
