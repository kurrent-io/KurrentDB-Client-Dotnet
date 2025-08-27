using System.Diagnostics;
using Kurrent.Client;

namespace KurrentDB.Diagnostics;

public static class KurrentActivitySource {
	static KurrentActivitySource() {
		var clientName    = AppVersionInfo.Current.ProductName ?? "KurrentDB .NET Client";
		var clientVersion = AppVersionInfo.Current.ProductVersion ?? AppVersionInfo.Current.FileVersion ?? "0.0.0";

		ActivitySource = new(clientName, clientVersion);
	}

	public static ActivitySource ActivitySource { get; }

	public static Activity? StartAppendActivity(ActivityTagsCollection tags) {
		if (!ActivitySource.HasListeners())
			return null;

		return StartActivity("multi-append", ActivityKind.Producer, tags);
	}

	static Activity? StartActivity(string operationName, ActivityKind kind, ActivityTagsCollection tags) {
		var activity = ActivitySource.StartActivity(kind, name: operationName, tags: tags);

		return activity;
	}
}
