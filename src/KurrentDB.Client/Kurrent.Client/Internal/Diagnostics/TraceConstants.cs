// ReSharper disable CheckNamespace

namespace KurrentDB.Diagnostics.Tracing;

static class TraceConstants {
	public const string MetadataTraceId = "$traceId";
	public const string MetadataSpanId  = "$spanId";

	public const string DatabaseUser      = "db.user";
	public const string DatabaseSystem    = "db.system";
	public const string DatabaseOperation = "db.operation";

	public const string ServerAddress       = "server.address";
	public const string ServerPort          = "server.port";
	public const string ServerSocketAddress = "server.socket.address";

	public const string ExceptionEventName  = "exception";
	public const string ExceptionType       = "exception.type";
	public const string ExceptionMessage    = "exception.message";
	public const string ExceptionStacktrace = "exception.stacktrace";

	public const string OtelStatusCode        = "otel.status_code";
	public const string OtelStatusDescription = "otel.status_description";
}
