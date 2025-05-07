using System.Diagnostics;

namespace KurrentDB.Client.Diagnostics;

public static class KurrentDBClientDiagnostics {
	public const string InstrumentationName = "kurrentdb";

	public static readonly ActivitySource ActivitySource = new(InstrumentationName);
}
