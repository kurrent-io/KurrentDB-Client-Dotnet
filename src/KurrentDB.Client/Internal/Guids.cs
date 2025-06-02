using System.Runtime.InteropServices;

namespace Kurrent.Client;

/// <summary>
/// Provides methods for generating GUIDs following the UUID version 7 specification as defined in RFC 9562.
/// UUID version 7 provides time-ordered identifiers with improved entropy, making them suitable for database
/// keys and other applications requiring sortable identifiers.
/// </summary>
/// <remarks>
/// If using .NET 9.0 or later, it utilizes the built-in `Guid.CreateVersion7` method for generating version 7 UUIDs.
/// </remarks>
static class Guids {
	/// <summary>
	/// Creates a new version 7 UUID using the current UTC timestamp.
	/// </summary>
	/// <remarks>
	/// If using .NET 9.0 or later, it uses the built-in method; otherwise, it falls back to a custom implementation.
	/// </remarks>
	/// <returns>A new GUID with UUID version 7 format using the current time.</returns>
	public static Guid CreateVersion7() {
#if NET9_0_OR_GREATER
		return Guid.CreateVersion7(DateTimeOffset.UtcNow);
#else
		return CreateVersion7(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
#endif
	}

	/// <summary>
	/// Creates a new version 7 UUID using the specified timestamp.
	/// </summary>
	/// <remarks>
	/// If using .NET 9.0 or later, it uses the built-in method; otherwise, it falls back to a custom implementation.
	/// </remarks>
	/// <param name="timestamp">The timestamp to use for the first 48 bits of the UUID.</param>
	/// <returns>A new GUID with UUID version 7 format using the provided timestamp.</returns>
	static Guid CreateVersion7(DateTimeOffset timestamp) {
#if NET9_0_OR_GREATER
		return Guid.CreateVersion7(timestamp);
#else
		return CreateVersion7(timestamp.ToUnixTimeMilliseconds());
#endif
	}

	/// <summary>
	/// Creates a new version 7 UUID using a raw millisecond timestamp value.
	/// </summary>
	/// <param name="timestamp">The Unix timestamp in milliseconds to use for the first 48 bits of the UUID.</param>
	/// <returns>A new GUID with UUID version 7 format.</returns>
	/// <remarks>
	/// The implementation follows RFC 9562 for UUID version 7:
	/// <list type="bullet">
	///   <item><description>First 48 bits: Unix timestamp in milliseconds (big-endian)</description></item>
	///   <item><description>Bits 49-52: UUID version (set to 7)</description></item>
	///   <item><description>Bits 53-64: Random bits</description></item>
	///   <item><description>Bits 65-66: UUID variant (set to RFC variant 0b10)</description></item>
	///   <item><description>Bits 67-128: Random bits</description></item>
	/// </list>
	///
	/// This generates time-ordered, unique identifiers suitable for use as database keys,
	/// reducing index fragmentation compared to random UUIDs.
	/// </remarks>
	static Guid CreateVersion7(long timestamp) {
		// Start with a random GUID to get high-quality random bits
		var guid = Guid.NewGuid();

		// Get direct access to the GUID's bytes without any allocation
		var guidBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref guid, 1));

		// Set the timestamp (big-endian) - first 48 bits (6 bytes)
		guidBytes[0] = (byte)(timestamp >> 40);
		guidBytes[1] = (byte)(timestamp >> 32);
		guidBytes[2] = (byte)(timestamp >> 24);
		guidBytes[3] = (byte)(timestamp >> 16);
		guidBytes[4] = (byte)(timestamp >> 8);
		guidBytes[5] = (byte)timestamp;

		// Set version to 7 (0111) in the 7th byte
		guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x70);

		// Set variant to RFC 9562 (10xx) in the 9th byte
		guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

		return guid;
	}
}
