namespace KurrentDB.Client {
	/// <summary>
	/// An interface representing the result of a write operation.
	/// </summary>
	public interface IWriteResult {
		/// <summary>
		/// The version the stream is currently at.
		/// </summary>
		[Obsolete("Please use NextExpectedStreamState instead. This property will be removed in a future version.", true)]
		long NextExpectedVersion { get; }

		/// <summary>
		/// The <see cref="Position"/> of the <see cref="IWriteResult"/> in the transaction file.
		/// </summary>
		Position LogPosition { get; }

		/// <summary>
		/// The <see cref="StreamState"/> the stream is currently at.
		/// </summary>
		StreamState NextExpectedStreamState { get; }
	}
}
