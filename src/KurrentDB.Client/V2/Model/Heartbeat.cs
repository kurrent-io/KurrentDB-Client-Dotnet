using System.Diagnostics.CodeAnalysis;

namespace Kurrent.Client.Model;

/// <summary>
/// Represents the type of a heartbeat message.
/// A heartbeat indicates progress or a significant event in a sequence of operations.
/// </summary>
public enum HeartbeatType {
	Unspecified = 0,
	Checkpoint  = 1,
	CaughtUp    = 2,
}

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
public readonly record struct Heartbeat(HeartbeatType Type, LogPosition Position, DateTimeOffset Timestamp) {
	/// <summary>
	/// Gets the type of the heartbeat, representing the nature of the event
	/// (e.g., indicating whether it is a checkpoint or a caught-up event).
	/// </summary>
	public required HeartbeatType Type { get; init; } = Type;

	/// <summary>
	/// Gets the position in the stream or sequence associated with this heartbeat.
	/// This indicates the progress or location of the event in the processing sequence.
	/// </summary>
	public required LogPosition Position { get; init; } = Position;

	/// <summary>
	/// Gets the timestamp indicating when the heartbeat message was generated.
	/// This provides temporal context for the event or progress it represents.
	/// </summary>
	public required DateTimeOffset Timestamp { get; init; } = Timestamp;

	/// <summary>
	/// Determines whether the heartbeat indicates a checkpoint event,
	/// signifying a specific milestone or save point in the processing sequence.
	/// </summary>
	public bool IsCheckpoint => Type == HeartbeatType.Checkpoint;

	/// <summary>
	/// Indicates whether the heartbeat represents a "CaughtUp" event, signaling that processing is up to date
	/// with the current state of the stream or sequence.
	/// </summary>
	public bool IsCaughtUp => Type == HeartbeatType.CaughtUp;

	public override string ToString() =>
		$"Heartbeat {{ Type = {Type}, Position = {Position}, Timestamp = {Timestamp:O} }}";

	public static Heartbeat CreateCheckpoint(LogPosition position, DateTimeOffset timestamp) =>
		new(HeartbeatType.Checkpoint, position, timestamp);

	public static Heartbeat CreateCaughtUp(LogPosition position, DateTimeOffset timestamp) =>
		new(HeartbeatType.CaughtUp, position, timestamp);
}
