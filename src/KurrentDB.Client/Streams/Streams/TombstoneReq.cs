using EventStore.Client;
using KurrentDB.Client;

namespace KurrentDB.Protocol.Streams.V1 {
	partial class TombstoneReq {
		public TombstoneReq WithAnyStreamRevision(StreamState expectedState) {
			if (expectedState == StreamState.Any) {
				Options.Any = new Empty();
			} else if (expectedState == StreamState.NoStream) {
				Options.NoStream = new Empty();
			} else if (expectedState == StreamState.StreamExists) {
				Options.StreamExists = new Empty();
			} else {
				Options.Revision = (ulong)expectedState.ToInt64();
			}

			return this;
		}
	}
}
