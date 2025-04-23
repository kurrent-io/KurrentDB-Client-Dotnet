using KurrentDB.Client;

namespace KurrentDB.Client.Tests;

static class MessageDataComparer {
	public static bool Equal(MessageData expected, EventRecord actual) {
		if (expected.MessageId != actual.EventId)
			return false;

		if (expected.Type != actual.EventType)
			return false;

		return expected.Data.ToArray().SequenceEqual(actual.Data.ToArray()) 
		    && expected.Metadata.ToArray().SequenceEqual(actual.Metadata.ToArray());
	}

	public static bool Equal(MessageData[] expected, EventRecord[] actual) {
		if (expected.Length != actual.Length)
			return false;

		return !expected.Where((t, i) => !Equal(t, actual[i])).Any();
	}
}
