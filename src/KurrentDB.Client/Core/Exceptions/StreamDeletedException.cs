namespace KurrentDB.Client {
	/// <summary>
	/// Exception thrown if an operation is attempted on a stream which
	/// has been deleted.
	/// </summary>
	public class StreamDeletedException : Exception {
		/// <summary>
		/// The name of the deleted stream.
		/// </summary>
		public readonly string Stream;

		/// <summary>
		/// Constructs a new instance of <see cref="StreamDeletedException"/>.
		/// </summary>
		/// <param name="stream">The name of the deleted stream.</param>
		/// <param name="exception"></param>
		public StreamDeletedException(string stream, Exception? exception = null)
			: base($"Event stream '{stream}' is deleted.", exception) {
			Stream = stream;
		}
	}
}
