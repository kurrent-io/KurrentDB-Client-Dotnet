using Kurrent.Variant;

namespace Kurrent.Client.Streams;

[PublicAPI]
public record ReadAllOptions : ReadOptionsBase {
    public LogPosition Start { get; init; } = LogPosition.Latest;

    public override void EnsureValid() {
        base.EnsureValid();
        ArgumentOutOfRangeException.ThrowIfLessThan(Start, LogPosition.Earliest);
    }

    internal static ReadAllOptions FirstRecord(CancellationToken cancellationToken = default) =>
        new() {
            Start             = LogPosition.Earliest,
            Direction         = ReadDirection.Forwards,
            Limit             = 1,
            Heartbeat         = HeartbeatOptions.Disabled,
            CancellationToken = cancellationToken
        };

    internal static ReadAllOptions LastRecord(CancellationToken cancellationToken = default) =>
        new() {
            Start             = LogPosition.Latest,
            Direction         = ReadDirection.Backwards,
            Limit             = 1,
            Heartbeat         = HeartbeatOptions.Disabled,
            CancellationToken = cancellationToken
        };
}

[PublicAPI]
public record ReadStreamOptions : ReadOptionsBase {
    public StreamName Stream { get; init; } = StreamName.None;

    public StreamRevision Start { get; init; } = StreamRevision.Min;

    public override void EnsureValid() {
        base.EnsureValid();

        ArgumentException.ThrowIfNullOrEmpty(Stream);
        ArgumentOutOfRangeException.ThrowIfLessThan(Start, StreamRevision.Min);

        if (Direction == ReadDirection.Forwards && Start == StreamRevision.Max)
            throw new ArgumentException(
                "Start revision cannot be Max when reading forwards. Use ReadDirection.Backwards or specify a valid revision.",
                nameof(Start)
            );
    }
}

public readonly partial record struct StartPosition : IVariant<LogPosition, StreamRevision> {
    // public LogPosition LogPosition { get; init; } = LogPosition.Latest;
    // public StreamRevision StreamRevision { get; init; } = StreamRevision.Min;
    //
    // public static ReadPosition Latest => new() { LogPosition = LogPosition.Latest };
    // public static ReadPosition Earliest => new() { LogPosition = LogPosition.Earliest };
    // public static ReadPosition Min => new() { StreamRevision = StreamRevision.Min };
    // public static ReadPosition Max => new() { StreamRevision = StreamRevision.Max };
    //
    // public static implicit operator ReadPosition(LogPosition logPosition) => new() { LogPosition = logPosition };
    // public static implicit operator ReadPosition(StreamRevision streamRevision) => new() { StreamRevision = streamRevision };
}

[PublicAPI]
public record ReadOptions {
    public LogPosition    Start { get; init; } = LogPosition.Latest;
    // public StreamRevision Start { get; init; } = StreamRevision.Min;

    // public StreamName Stream { get; init; } = StreamName.None;


    public ReadFilter Filter { get; init; } = ReadFilter.None;

    public HeartbeatOptions Heartbeat { get; init; } = HeartbeatOptions.Default;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public int BufferSize { get; init; } = 1000;

    public ReadDirection Direction { get; init; } = ReadDirection.Forwards;

    public long Limit { get; init; } = long.MaxValue;

    public bool SkipDecoding { get; init; }

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}

[PublicAPI]
public abstract record ReadOptionsBase {
    public bool SkipDecoding { get; init; }

    public ReadFilter Filter { get; init; } = ReadFilter.None;

    public HeartbeatOptions Heartbeat { get; init; } = HeartbeatOptions.Default;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    public int BufferSize { get; init; } = 1000;

    public ReadDirection Direction { get; init; } = ReadDirection.Forwards;

    public long Limit { get; init; } = long.MaxValue;

    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    public virtual void EnsureValid() {
        if (Heartbeat.Enable) {
            ArgumentOutOfRangeException.ThrowIfLessThan(Heartbeat.RecordsThreshold, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(Heartbeat.RecordsThreshold, 10000);
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(Timeout, TimeSpan.FromSeconds(1));

        ArgumentOutOfRangeException.ThrowIfLessThan(BufferSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(BufferSize, 10000);

        ArgumentOutOfRangeException.ThrowIfLessThan(Limit, 1);
    }
}
