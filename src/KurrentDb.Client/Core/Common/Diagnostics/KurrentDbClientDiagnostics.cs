using System.Diagnostics;

namespace KurrentDb.Client.Diagnostics;

public static class KurrentDbClientDiagnostics {
	public const           string         InstrumentationName = "kurrent";
	public static readonly ActivitySource ActivitySource      = new(InstrumentationName);
}
