using System.Text;
using Google.Protobuf;

namespace EventStore.Client;

// This is pure insanity and we are counting the days to implement V2 of the protocol
// where this will be a string and not a byte array, bringing peace to the world.
partial class StreamIdentifier {
	string? _cached;

	public static implicit operator string?(StreamIdentifier? source) {
		if (source == null) return null;

		if (source._cached != null || source.StreamName.IsEmpty) return source._cached;

		var tmp = Encoding.UTF8.GetString(source.StreamName.Span);

		//this doesn't have to be thread safe, its just a cache in case the identifier is turned into a string several times
		source._cached = tmp;
		return source._cached;
	}

	public static implicit operator StreamIdentifier(string source) =>
		new() { StreamName = ByteString.CopyFromUtf8(source) };
}
