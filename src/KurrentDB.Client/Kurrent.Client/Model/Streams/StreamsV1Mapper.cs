#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

// ReSharper disable CheckNamespace

using Contracts = EventStore.Client.Streams;

namespace Kurrent.Client.Model;

static class StreamsV1Mapper {
    static readonly Contracts.ReadReq.Types.Options.Types.ControlOption       DefaultControlOption       = new() { Compatibility = 1 };
    static readonly Contracts.ReadReq.Types.Options.Types.SubscriptionOptions DefaultSubscriptionOptions = new();
    static readonly Contracts.ReadReq.Types.Options.Types.UUIDOption          DefaultUuidOptions         = new();
    static readonly EventStore.Client.Empty                                   DefaultEmpty               = new();

    #region . requests .

    public static Contracts.ReadReq CreateSubscriptionRequest(AllSubscriptionOptions options) {
        return NewSubscriptionRequest()
            .With(x => x.Options.All = ConvertToAllOptions(options.Start))
            .With(x => x.Options.Filter = ConvertToFilterOptions(options.Filter, options.Heartbeat));
    }

    public static Contracts.ReadReq CreateStreamSubscriptionRequest(StreamSubscriptionOptions options) {
        return NewSubscriptionRequest()
            .With(x => x.Options.Stream = ConvertToStreamOptions(options.Stream, options.Start));
    }

    static Contracts.ReadReq NewSubscriptionRequest() =>
        new() {
            Options = new() {
                ReadDirection = Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                Subscription  = DefaultSubscriptionOptions,
                UuidOption    = DefaultUuidOptions,
                NoFilter      = DefaultEmpty
            }
        };

    public static Contracts.ReadReq CreateReadRequest(ReadAllOptions options) {
        return new Contracts.ReadReq {
            Options = new() {
                UuidOption    = DefaultUuidOptions,
                ControlOption = DefaultControlOption,
                ReadDirection = options.Direction switch {
                    ReadDirection.Backwards => Contracts.ReadReq.Types.Options.Types.ReadDirection.Backwards,
                    ReadDirection.Forwards  => Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                },
                All = new() { Position = new() {
                    CommitPosition  = options.Start,
                    PreparePosition = options.Start
                } },
                Count  = (ulong)options.Limit,
                Filter = ConvertToFilterOptions(options.Filter, options.Heartbeat)
            }
        };
    }

    public static Contracts.ReadReq CreateReadRequest(ReadStreamOptions options) {
        return new Contracts.ReadReq {
            Options = new() {
                UuidOption    = DefaultUuidOptions,
                ControlOption = DefaultControlOption,
                NoFilter      = DefaultEmpty,
                ReadDirection = options.Direction switch {
                    ReadDirection.Backwards => Contracts.ReadReq.Types.Options.Types.ReadDirection.Backwards,
                    ReadDirection.Forwards  => Contracts.ReadReq.Types.Options.Types.ReadDirection.Forwards,
                },
                Stream = ConvertToStreamOptions(options.Stream, options.Start),
                Count  = (ulong)options.Limit,
            }
        };
    }

    public static Contracts.DeleteReq CreateDeleteRequest(StreamName stream, ExpectedStreamState expectedState) {
        var req = new Contracts.DeleteReq { Options = new() { StreamIdentifier = stream.Value } };
        return expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => req.With(x => x.Options.Any = DefaultEmpty),
            _ when expectedState == ExpectedStreamState.NoStream     => req.With(x => x.Options.NoStream = DefaultEmpty),
            _ when expectedState == ExpectedStreamState.StreamExists => req.With(x => x.Options.StreamExists = DefaultEmpty),
            _                                                        => req.With(x => x.Options.Revision = expectedState)
        };
    }

    public static Contracts.TombstoneReq CreateTombstoneRequest(StreamName stream, ExpectedStreamState expectedState) {
        var req = new Contracts.TombstoneReq { Options = new() { StreamIdentifier = stream.Value } };
        return expectedState switch {
            _ when expectedState == ExpectedStreamState.Any          => req.With(x => x.Options.Any = DefaultEmpty),
            _ when expectedState == ExpectedStreamState.NoStream     => req.With(x => x.Options.NoStream = DefaultEmpty),
            _ when expectedState == ExpectedStreamState.StreamExists => req.With(x => x.Options.StreamExists = DefaultEmpty),
            _                                                        => req.With(x => x.Options.Revision = expectedState)
        };
    }

    #endregion

    #region . convert .

    static Contracts.ReadReq.Types.Options.Types.FilterOptions? ConvertToFilterOptions(ReadFilter filter, HeartbeatOptions heartbeat) {
        if (filter == ReadFilter.None)
            return null;

        var options = filter.Scope switch {
            ReadFilterScope.Stream => new Contracts.ReadReq.Types.Options.Types.FilterOptions {
                StreamIdentifier = new() { Regex = filter.Expression }
            },
            ReadFilterScope.Record => new Contracts.ReadReq.Types.Options.Types.FilterOptions {
                EventType = new() { Regex = filter.Expression }
            },
        };

        // what is this?!
        //options.Count = DefaultEmpty;
        // if (filter.MaxSearchWindow.HasValue)
        //     options.Max = (uint)heartbeatRecordsThreshold;
        // else
        //     options.Count = DefaultEmpty;

        options.Max                          = (uint)heartbeat.RecordsThreshold;
        options.CheckpointIntervalMultiplier = 1;

        return options;
    }

    static Contracts.ReadReq.Types.Options.Types.AllOptions ConvertToAllOptions(LogPosition start) =>
        start switch {
            _ when start == LogPosition.Latest   => new() { End      = DefaultEmpty },
            _ when start == LogPosition.Earliest => new() { Start    = DefaultEmpty },
            _                                    => new() { Position = new() { CommitPosition = start, PreparePosition = start } }
        };

    static Contracts.ReadReq.Types.Options.Types.StreamOptions ConvertToStreamOptions(string stream, StreamRevision start) =>
        start switch {
            _ when start == StreamRevision.Max => new() { StreamIdentifier = stream, End      = DefaultEmpty },
            _ when start == StreamRevision.Min => new() { StreamIdentifier = stream, Start    = DefaultEmpty },
            _                                  => new() { StreamIdentifier = stream, Revision = (ulong)start.Value }
        };

    #endregion

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.Checkpoint checkpoint) {
        var position  = LogPosition.From((long)checkpoint.CommitPosition);
        var timestamp = checkpoint.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCheckpoint(position, timestamp);
    }

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.CaughtUp caughtUp) {
        var position  = caughtUp.Position is not null ? LogPosition.From((long)caughtUp.Position.CommitPosition) : LogPosition.Unset;
        var revision  = caughtUp.HasStreamRevision ? StreamRevision.From(caughtUp.StreamRevision) : StreamRevision.Unset;
        var timestamp = caughtUp.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateCaughtUp(position, revision, timestamp);
    }

    public static Heartbeat MapToHeartbeat(this Contracts.ReadResp.Types.FellBehind fellBehind) {
        var position  = fellBehind.Position is not null ? LogPosition.From((long)fellBehind.Position.CommitPosition) : LogPosition.Unset;
        var revision  = fellBehind.HasStreamRevision ? StreamRevision.From(fellBehind.StreamRevision) : StreamRevision.Unset;
        var timestamp = fellBehind.Timestamp.ToDateTimeOffset();
        return Heartbeat.CreateFellBehind(position, revision, timestamp);
    }

    public static LogPosition MapToLogPosition(this Contracts.DeleteResp.Types.Position? position) =>
        position switch {
            null => LogPosition.Unset,
            _    => LogPosition.From((long)position.CommitPosition)
        };

    public static LogPosition MapToLogPosition(this Contracts.TombstoneResp.Types.Position? position) =>
        position switch {
            null => LogPosition.Unset,
            _    => LogPosition.From((long)position.CommitPosition)
        };
}
