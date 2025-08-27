using System.Diagnostics;

namespace Kurrent.Client.Streams;

public partial record Messages {
	internal void CompleteActivity(Record record) {
		Activities.TryRemove(record.Id, out var activity);
		activity?.SetStatus(ActivityStatusCode.Ok);
		activity?.Dispose();
	}
}
