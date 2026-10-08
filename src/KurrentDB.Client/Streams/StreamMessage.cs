namespace KurrentDB.Client {
	/// <summary>
	/// The base record of all stream messages.
	/// </summary>
	public abstract record StreamMessage {
		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> that represents a <see cref="KurrentDB.Client.ResolvedEvent"/>.
		/// </summary>
		/// <param name="ResolvedEvent">The <see cref="KurrentDB.Client.ResolvedEvent"/>.</param>
		public record Event(ResolvedEvent ResolvedEvent) : StreamMessage;

		/// <summary>
		/// A <see cref="StreamMessage"/> representing a stream that was not found.
		/// </summary>
		public record NotFound : StreamMessage {
			internal static readonly NotFound Instance = new();
		}

		/// <summary>
		/// A <see cref="StreamMessage"/> representing a successful read operation.
		/// </summary>
		public record Ok : StreamMessage {
			internal static readonly Ok Instance = new();
		};

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating the first position of a stream.
		/// </summary>
		/// <param name="StreamPosition">The <see cref="KurrentDB.Client.StreamPosition"/>.</param>
		public record FirstStreamPosition(StreamPosition StreamPosition) : StreamMessage;

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating the last position of a stream.
		/// </summary>
		/// <param name="StreamPosition">The <see cref="KurrentDB.Client.StreamPosition"/>.</param>
		public record LastStreamPosition(StreamPosition StreamPosition) : StreamMessage;

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating the last position of the $all stream.
		/// </summary>
		/// <param name="Position">The <see cref="KurrentDB.Client.Position"/>.</param>
		public record LastAllStreamPosition(Position Position) : StreamMessage;

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating that the subscription is ready to send additional messages.
		/// </summary>
		/// <param name="SubscriptionId">The unique identifier of the subscription.</param>
		public record SubscriptionConfirmation(string SubscriptionId) : StreamMessage;

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating that a checkpoint has been reached.
		/// </summary>
		/// <param name="Position">The <see cref="Position" />.</param>
		public record AllStreamCheckpointReached(Position Position) : StreamMessage;
		
		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating that a checkpoint has been reached.
		/// </summary>
		/// <param name="StreamPosition">The <see cref="StreamPosition" />.</param>
		public record StreamCheckpointReached(StreamPosition StreamPosition) : StreamMessage;

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating that the subscription is live.
		/// </summary>
		/// <remarks>
		/// With <see cref="KurrentDBClientSettings.EnableRichSubscriptionLiveness"/>, this is a
		/// <see cref="StreamCaughtUp"/> or <see cref="AllStreamCaughtUp"/>.
		/// </remarks>
		public record CaughtUp : StreamMessage {
			internal static readonly CaughtUp Instance = new();
		}

		/// <summary>
		/// A <see cref="CaughtUp"/> for a stream subscription.
		/// </summary>
		public sealed record StreamCaughtUp : CaughtUp {
			/// <summary>
			/// The server's clock (UTC) when the subscription caught up. May be absent on older servers.
			/// </summary>
			public DateTime? Timestamp { get; init; }

			/// <summary>
			/// The last event the subscription has sent, or its start position if none yet. Absent only when it
			/// started from the beginning and has sent nothing, or the server does not send it.
			/// </summary>
			public StreamPosition? StreamPosition { get; init; }
		}

		/// <summary>
		/// A <see cref="CaughtUp"/> for a subscription to $all.
		/// </summary>
		public sealed record AllStreamCaughtUp : CaughtUp {
			/// <summary>
			/// The server's clock (UTC) when the subscription caught up. May be absent on older servers.
			/// </summary>
			public DateTime? Timestamp { get; init; }

			/// <summary>
			/// The last event or checkpoint the subscription has sent, or its start position if none yet. Absent
			/// only when it started from the beginning and has sent nothing, or the server does not send it.
			/// </summary>
			public Position? Position { get; init; }
		}

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> indicating that the subscription has switched to catch up mode.
		/// </summary>
		/// <remarks>
		/// Only received with <see cref="KurrentDBClientSettings.EnableRichSubscriptionLiveness"/>, as a
		/// <see cref="StreamFellBehind"/> or <see cref="AllStreamFellBehind"/>.
		/// </remarks>
		public record FellBehind : StreamMessage {
			internal static readonly FellBehind Instance = new();
		}

		/// <summary>
		/// A <see cref="FellBehind"/> for a stream subscription.
		/// </summary>
		public sealed record StreamFellBehind : FellBehind {
			/// <summary>
			/// The server's clock (UTC) when the subscription fell behind. May be absent on older servers.
			/// </summary>
			public DateTime? Timestamp { get; init; }

			/// <summary>
			/// The last event the subscription has sent, or its start position if none yet. Absent only when it
			/// started from the beginning and has sent nothing, or the server does not send it.
			/// </summary>
			public StreamPosition? StreamPosition { get; init; }
		}

		/// <summary>
		/// A <see cref="FellBehind"/> for a subscription to $all.
		/// </summary>
		public sealed record AllStreamFellBehind : FellBehind {
			/// <summary>
			/// The server's clock (UTC) when the subscription fell behind. May be absent on older servers.
			/// </summary>
			public DateTime? Timestamp { get; init; }

			/// <summary>
			/// The last event or checkpoint the subscription has sent, or its start position if none yet. Absent
			/// only when it started from the beginning and has sent nothing, or the server does not send it.
			/// </summary>
			public Position? Position { get; init; }
		}

		/// <summary>
		/// A <see cref="KurrentDB.Client.StreamMessage"/> that could not be identified, usually indicating a lower client compatibility level than the server supports.
		/// </summary>
		public record Unknown : StreamMessage {
			internal static readonly Unknown Instance = new();
		}
	}
}
