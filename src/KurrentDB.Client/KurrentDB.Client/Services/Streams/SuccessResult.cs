// namespace KurrentDB.Client;
//
// /// <summary>
// /// An <see cref="IWriteResult"/> that indicates a successful append to a stream.
// /// </summary>
// public readonly struct SuccessResult : IWriteResult, IEquatable<SuccessResult> {
// 	/// <inheritdoc />
// 	public long NextExpectedVersion { get; }
//
// 	/// <inheritdoc />
// 	public Position LogPosition { get; }
//
// 	/// <inheritdoc />
// 	public StreamState NextExpectedStreamState { get; }
//
// 	/// <summary>
// 	/// Constructs a new <see cref="SuccessResult"/>.
// 	/// </summary>
// 	/// <param name="nextExpectedStreamState"></param>
// 	/// <param name="logPosition"></param>
// 	public SuccessResult(StreamState nextExpectedStreamState, Position logPosition) {
// 		NextExpectedStreamState = nextExpectedStreamState;
// 		LogPosition             = logPosition;
// 		NextExpectedVersion     = nextExpectedStreamState.ToInt64();
// 	}
//
// 	/// <inheritdoc />
// 	public bool Equals(SuccessResult other) =>
// 		NextExpectedStreamState == other.NextExpectedStreamState && LogPosition.Equals(other.LogPosition);
//
// 	/// <inheritdoc />
// 	public override bool Equals(object? obj) => obj is SuccessResult other && Equals(other);
//
// 	/// <summary>
// 	/// Compares left and right for equality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is equal to right.</returns>
// 	public static bool operator ==(SuccessResult left, SuccessResult right) => left.Equals(right);
//
// 	/// <summary>
// 	/// Compares left and right for inequality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is equal not to right.</returns>
// 	public static bool operator !=(SuccessResult left, SuccessResult right) => !left.Equals(right);
//
// 	/// <inheritdoc />
// 	public override int GetHashCode() => HashCode.Hash.Combine(NextExpectedVersion).Combine(LogPosition);
//
// 	/// <inheritdoc />
// 	public override string ToString() => $"{NextExpectedStreamState}:{LogPosition}";
// }
