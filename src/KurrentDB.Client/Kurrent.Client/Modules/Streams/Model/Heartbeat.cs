using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Streams;

/// <summary>
/// Represents a heartbeat message providing information about the state or progress of processing.
/// </summary>
/// <param name="Type">
/// The type of the heartbeat, indicating the nature of the event (e.g., checkpoint or caught-up).
/// </param>
/// <param name="Position">
/// The position in the stream or sequence associated with this heartbeat.
/// </param>
/// <param name="Timestamp">
/// The timestamp indicating when this heartbeat was generated.
/// </param>
[PublicAPI]
[method: SetsRequiredMembers]
public readonly record struct Heartbeat(HeartbeatType Type, LogPosition Position, StreamRevision StreamRevision, DateTimeOffset Timestamp) {
	public static readonly Heartbeat None = new(HeartbeatType.Unspecified, LogPosition.Unset, StreamRevision.Unset, DateTimeOffset.MinValue);

	/// <summary>
	/// The type of the heartbeat, representing the nature of the event
	/// (e.g. indicating whether it is a checkpoint or a caught-up event).
	/// </summary>
	public required HeartbeatType Type { get; init; } = Type;

	/// <summary>
	/// The position in the stream or sequence associated with this heartbeat.
	/// This indicates the progress or location of the event in the processing sequence.
	/// </summary>
	public required LogPosition Position { get; init; } = Position;

	/// <summary>
	/// Represents a specific point in the stream or sequence.
	/// </summary>
	public required StreamRevision StreamRevision { get; init; } = StreamRevision;

	/// <summary>
	/// The timestamp indicating when this heartbeat message was generated.
	/// </summary>
	public required DateTimeOffset Timestamp { get; init; } = Timestamp;

	/// <summary>
	/// Determines whether the heartbeat indicates a checkpoint event,
	/// signifying a specific milestone or save point in the processing sequence.
	/// </summary>
	public bool IsCheckpoint => Type == HeartbeatType.Checkpoint;

	/// <summary>
	/// Indicates whether the heartbeat represents a "CaughtUp" event, signalling that processing is up to date
	/// with the current state of the stream or sequence.
	/// </summary>
	public bool IsCaughtUp => Type == HeartbeatType.CaughtUp;

	/// <summary>
	/// Indicates whether the heartbeat represents a "fell behind" event,
	/// meaning the process is no longer caught up with the expected stream position.
	/// </summary>
	public bool IsFellBehind => Type == HeartbeatType.FellBehind;

	public bool HasPosition => Position != LogPosition.Unset;

	public bool HasStreamRevision => StreamRevision != StreamRevision.Unset;

	public override string ToString() =>
		$"Heartbeat {{ Type = {Type}, Position = {Position}, StreamRevision = {StreamRevision}, Timestamp = {Timestamp:O} }}";

	public static Heartbeat CreateCheckpoint(LogPosition position, DateTimeOffset timestamp) =>
		new(HeartbeatType.Checkpoint, position, StreamRevision.Unset, timestamp);

	public static Heartbeat CreateCaughtUp(LogPosition position, DateTimeOffset timestamp) =>
		new(HeartbeatType.CaughtUp, position, StreamRevision.Unset,  timestamp);

	public static Heartbeat CreateFellBehind(LogPosition position, DateTimeOffset timestamp) =>
		new(HeartbeatType.FellBehind, position, StreamRevision.Unset,  timestamp);

	public static Heartbeat CreateCheckpoint(LogPosition position, StreamRevision streamRevision, DateTimeOffset timestamp) =>
		new(HeartbeatType.Checkpoint, position, streamRevision, timestamp);

	public static Heartbeat CreateCaughtUp(LogPosition position, StreamRevision streamRevision, DateTimeOffset timestamp) =>
		new(HeartbeatType.CaughtUp, position, streamRevision, timestamp);

	public static Heartbeat CreateFellBehind(LogPosition position, StreamRevision streamRevision, DateTimeOffset timestamp) =>
		new(HeartbeatType.FellBehind, position, streamRevision, timestamp);
}

/// <summary>
/// Represents the type of heartbeat message.
/// A heartbeat indicates progress or a significant event in a sequence of operations.
/// </summary>
public enum HeartbeatType {
    Unspecified = 0,
    Checkpoint  = 1,
    CaughtUp    = 2,
    FellBehind  = 3,
}
