using connecting_to_a_single_node;
using KurrentDB.Client;

#pragma warning disable CS8321 // Local function is declared but never used

static void SimpleConnection() {
	#region creating-simple-connection

	using var client = new KurrentDBClient(KurrentDBClientSettings.Create("kurrentdb://localhost:2113"));

	#endregion creating-simple-connection
}

static void ProvidingDefaultCredentials() {
	#region providing-default-credentials

	using var client = new KurrentDBClient(KurrentDBClientSettings.Create("esdb://admin:changeit@localhost:2113"));

	#endregion providing-default-credentials
}

static void SpecifyingAConnectionName() {
	#region setting-the-connection-name

	using var client = new KurrentDBClient(
		KurrentDBClientSettings.Create("esdb://admin:changeit@localhost:2113?ConnectionName=SomeConnection")
	);

	#endregion setting-the-connection-name
}

static void OverridingTheTimeout() {
	#region overriding-timeout

	using var client = new KurrentDBClient(
		KurrentDBClientSettings.Create($"esdb://admin:changeit@localhost:2113?OperationTimeout=30000")
	);

	#endregion overriding-timeout
}

static void CombiningSettings() {
	#region overriding-timeout

	using var client = new KurrentDBClient(
		KurrentDBClientSettings.Create(
			$"esdb://admin:changeit@localhost:2113?ConnectionName=SomeConnection&OperationTimeout=30000"
		)
	);

	#endregion overriding-timeout
}

static void CreatingAnInterceptor() {
	#region adding-an-interceptor

	var settings = new KurrentDBClientSettings {
		Interceptors = new[] { new DemoInterceptor() },
		ConnectivitySettings = {
			Address = new Uri("https://localhost:2113")
		}
	};

	#endregion adding-an-interceptor

	var client = new KurrentDBClient(settings);
}

static void CustomHttpMessageHandler() {
	#region adding-an-custom-http-message-handler

	var settings = new KurrentDBClientSettings {
		CreateHttpMessageHandler = () =>
			new HttpClientHandler {
				ServerCertificateCustomValidationCallback =
					(sender, cert, chain, sslPolicyErrors) => true
			},
		ConnectivitySettings = {
			Address = new Uri("https://localhost:2113")
		}
	};

	#endregion adding-an-custom-http-message-handler

	var client = new KurrentDBClient(settings);
}
