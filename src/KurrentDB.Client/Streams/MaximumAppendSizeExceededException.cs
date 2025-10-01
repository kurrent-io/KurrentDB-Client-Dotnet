using System;

namespace KurrentDB.Client {
	/// <summary>
	/// Exception thrown when an append exceeds the maximum size set by the server.
	/// </summary>
	public class AppendRecordSizeExceededException : Exception {
		/// <summary>
		/// The name of the stream where the append was attempted.
		/// </summary>
		public string Stream { get; }

		/// <summary>
		/// The identifier of the offending and oversized record.
		/// </summary>
		public string RecordId { get; }

		/// <summary>
		/// The size of the huge record in bytes.
		/// </summary>
		public long Size { get; }

		/// <summary>
		/// The maximum allowed size of a single record that can be appended in bytes.
		/// </summary>
		public long MaxSize { get; }

		/// <summary>
		/// Constructs a new <see cref="AppendRecordSizeExceededException"/>.
		/// </summary>
		/// <param name="stream">The name of the stream where the append was attempted.</param>
		/// <param name="recordId">The identifier of the offending and oversized record.</param>
		/// <param name="size">The size of the huge record in bytes.</param>
		/// <param name="maxSize">The maximum allowed size of a single record that can be appended in bytes.</param>
		/// <param name="innerException">The inner exception, if any.</param>
		public AppendRecordSizeExceededException(string stream, string recordId, long size, long maxSize, Exception? innerException = null)
			: base($"The size of the record {recordId} ({size}) exceeds by maximum allowed size of {maxSize} bytes by {size - maxSize}", innerException) {
			Stream   = stream;
			RecordId = recordId;
			Size     = size;
			MaxSize  = maxSize;
		}
	}
}
