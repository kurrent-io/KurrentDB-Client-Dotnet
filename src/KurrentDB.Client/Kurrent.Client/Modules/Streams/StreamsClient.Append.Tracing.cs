using System.Diagnostics;
using Grpc.Core;

namespace Kurrent.Client.Streams;

public static partial class StreamsClientTracing {
	public static void FailActivity(this Activity? activity, RpcException exception) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested) {
			activity.AddException(exception);
			activity.SetStatus(ActivityStatusCode.Error);
		}

		activity.Dispose();
	}

	public static void FailActivity(this Activity? activity, AppendStreamFailures failures) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested) {
			failures.ForEach(failure => activity.AddException(failure.CreateException()));
			activity.SetStatus(ActivityStatusCode.Error);
		}

		activity.Dispose();
	}

	public static void CompleteActivity(this Activity? activity) {
		if (activity is null)
			return;

		if (activity.IsAllDataRequested)
			activity.SetStatus(ActivityStatusCode.Ok);

		activity.Dispose();
	}
}
