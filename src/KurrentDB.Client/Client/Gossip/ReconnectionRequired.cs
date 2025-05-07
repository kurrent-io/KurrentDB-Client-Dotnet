using System.Net;

namespace KurrentDB.Client;

abstract record ReconnectionRequired {
	public record None : ReconnectionRequired {
		public static None Instance = new();
	}

	public record Rediscover : ReconnectionRequired {
		public static Rediscover Instance = new();
	}

	public record NewLeader(DnsEndPoint EndPoint) : ReconnectionRequired;
}