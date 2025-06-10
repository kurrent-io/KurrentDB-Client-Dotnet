namespace KurrentDB.Client;

public readonly struct AppendStreamSuccess(string stream, long position, StreamState nextStreamState) {
	public string      Stream          { get; } = stream;
	public long        Position        { get; } = position;
	public StreamState NextStreamState { get; } = nextStreamState;
}
