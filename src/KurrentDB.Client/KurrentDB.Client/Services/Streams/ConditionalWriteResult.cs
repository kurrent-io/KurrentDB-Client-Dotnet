// namespace KurrentDB.Client;
//
// /// <summary>
// /// A structure that represents the result of a conditional write.
// /// </summary>
// public readonly struct ConditionalWriteResult : IEquatable<ConditionalWriteResult> {
// 	/// <summary>
// 	/// Indicates that the stream the operation is targeting was deleted.
// 	/// </summary>
// 	public static readonly ConditionalWriteResult StreamDeleted =
// 		new ConditionalWriteResult(StreamState.NoStream, Position.End, ConditionalWriteStatus.StreamDeleted);
//
// 	/// <summary>
// 	/// The correct expected version to use when writing to the stream next.
// 	/// </summary>
// 	public long NextExpectedVersion { get; }
//
// 	/// <summary>
// 	/// The <see cref="Position"/> of the write in the transaction file.
// 	/// </summary>
// 	public Position LogPosition { get; }
//
// 	/// <summary>
// 	/// The <see cref="ConditionalWriteStatus"/>.
// 	/// </summary>
// 	public ConditionalWriteStatus Status { get; }
//
// 	/// <summary>
// 	/// The correct <see cref="StreamState"/> to use when writing to the stream next.
// 	/// </summary>
// 	public StreamState NextExpectedStreamState { get; }
//
// 	ConditionalWriteResult(StreamState nextExpectedStreamState, Position logPosition,
// 	                       ConditionalWriteStatus status = ConditionalWriteStatus.Succeeded) {
// 		NextExpectedStreamState = nextExpectedStreamState;
// 		NextExpectedVersion     = nextExpectedStreamState.ToInt64();
// 		LogPosition             = logPosition;
// 		Status                  = status;
// 	}
//
// 	internal static ConditionalWriteResult FromWriteResult(IWriteResult writeResult)
// 		=> writeResult switch {
// 			WrongExpectedVersionResult wrongExpectedVersion =>
// 				new ConditionalWriteResult(wrongExpectedVersion.NextExpectedStreamState, Position.End,
// 					ConditionalWriteStatus.VersionMismatch),
// 			_ => new ConditionalWriteResult(writeResult.NextExpectedStreamState, writeResult.LogPosition)
// 		};
//
// 	internal static ConditionalWriteResult FromWrongExpectedVersion(WrongExpectedVersionException ex)
// 		=> new ConditionalWriteResult(ex.ExpectedStreamState, Position.End,
// 			ConditionalWriteStatus.VersionMismatch);
//
// 	/// <inheritdoc />
// 	public bool Equals(ConditionalWriteResult other) =>
// 		NextExpectedStreamState == other.NextExpectedStreamState &&
// 		LogPosition.Equals(other.LogPosition) &&
// 		Status == other.Status;
//
// 	/// <inheritdoc />
// 	public override bool Equals(object? obj) => obj is ConditionalWriteResult other && Equals(other);
//
// 	/// <inheritdoc />
// 	public override int GetHashCode() =>
// 		HashCode.Hash.Combine(NextExpectedVersion).Combine(LogPosition).Combine(Status);
//
// 	/// <summary>
// 	/// Compares left and right for equality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is equal to right.</returns>
// 	public static bool operator ==(ConditionalWriteResult left, ConditionalWriteResult right) => left.Equals(right);
//
// 	/// <summary>
// 	/// Compares left and right for inequality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is not equal to right.</returns>
// 	public static bool operator !=(ConditionalWriteResult left, ConditionalWriteResult right) =>
// 		!left.Equals(right);
//
// 	/// <inheritdoc />
// 	public override string ToString() => $"{Status}:{NextExpectedVersion}:{LogPosition}";
// }
