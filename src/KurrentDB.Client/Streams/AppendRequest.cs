namespace KurrentDB.Client;

public record AppendRequest(string StreamName, StreamState ExpectedState, IEnumerable<Message> Messages);
