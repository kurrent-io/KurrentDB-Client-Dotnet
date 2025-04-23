using System.Diagnostics;

namespace KurrentDB.Client.Diagnostics;

public static class KurrentDBClientDiagnostics {
	public const           string         InstrumentationName = "kurrent";
	public static readonly ActivitySource ActivitySource      = new(InstrumentationName);
}
