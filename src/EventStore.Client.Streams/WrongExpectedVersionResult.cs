namespace EventStore.Client {
	/// <summary>
	/// An <see cref="IWriteResult"/> that indicates a failed append to a stream.
	/// </summary>
	public readonly struct WrongExpectedVersionResult : IWriteResult {
		/// <summary>
		/// The name of the stream.
		/// </summary>
		public string StreamName { get; }

		/// <inheritdoc />
		public long NextExpectedVersion { get; }

		/// <summary>
		/// The version the stream is at.
		/// </summary>
		public long ActualVersion { get; }

		/// <summary>
		/// The <see cref="StreamState"/> the stream is at.
		/// </summary>
		public StreamState ActualStreamState { get; }

		/// <inheritdoc />
		public Position LogPosition { get; }

		/// <inheritdoc />
		public StreamState NextExpectedStreamState { get; }

		/// <summary>
		/// Construct a new <see cref="WrongExpectedVersionResult"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="nextExpectedStreamState"></param>
		public WrongExpectedVersionResult(string streamName, StreamState nextExpectedStreamState) {
			StreamName = streamName;
			ActualVersion = NextExpectedVersion = nextExpectedStreamState.ToInt64();
			ActualStreamState = NextExpectedStreamState = nextExpectedStreamState;
			LogPosition = default;
		}

		/// <summary>
		/// Construct a new <see cref="WrongExpectedVersionResult"/>.
		/// </summary>
		/// <param name="streamName"></param>
		/// <param name="nextExpectedStreamState"></param>
		/// <param name="actualStreamState"></param>
		public WrongExpectedVersionResult(string streamName, StreamState nextExpectedStreamState,
			StreamState actualStreamState) {
			StreamName = streamName;
			ActualVersion = actualStreamState.ToInt64();
			ActualStreamState = actualStreamState;
			NextExpectedVersion = nextExpectedStreamState.ToInt64();
			NextExpectedStreamState = nextExpectedStreamState;
			LogPosition = default;
		}
	}
}
