using System.Net.Http;
using System.Reflection;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging.Abstractions;

namespace KurrentDB.Client.Tests;

[Trait("Category", "Target:Misc")]
public class KurrentDBClientSettingsTests {
	static KurrentDBClientSettings CreateSettings() => new() {
		Interceptors             = new List<Interceptor> { new TestInterceptor() },
		ConnectionName           = "connection",
		CreateHttpMessageHandler = () => new HttpClientHandler(),
		LoggerFactory            = NullLoggerFactory.Instance,
		ChannelCredentials       = ChannelCredentials.Insecure,
		OperationOptions         = new() { BatchAppendSize = 1 },
		ConnectivitySettings     = new() { MaxDiscoverAttempts = 1 },
		DefaultCredentials       = new("user", "password"),
		DefaultDeadline          = TimeSpan.FromSeconds(3),
		EnableRichSubscriptionLiveness = true
	};

	static readonly PropertyInfo[] Properties = typeof(KurrentDBClientSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

	[Fact]
	public void rich_subscription_liveness_is_disabled_by_default() {
		Assert.False(new KurrentDBClientSettings().EnableRichSubscriptionLiveness);
		Assert.False(KurrentDBClientSettings.Create("kurrentdb://localhost:2113?tls=false").EnableRichSubscriptionLiveness);
	}

	[Fact]
	public void clone_copies_every_setting() {
		var settings = CreateSettings();
		var defaults = new KurrentDBClientSettings();
		var clone    = settings.Clone();

		Assert.NotSame(settings, clone);
		Assert.All(
			Properties,
			property => {
				// a value left at its default would not show that it is copied
				Assert.NotEqual(property.GetValue(defaults), property.GetValue(settings));
				AssertCopied(property, settings, clone);
			}
		);
	}

	// operation options and interceptors are copied, so compare them by content
	static void AssertCopied(PropertyInfo property, KurrentDBClientSettings settings, KurrentDBClientSettings copy) {
		if (property.Name == nameof(KurrentDBClientSettings.Interceptors)) {
			Assert.NotSame(settings.Interceptors, copy.Interceptors);
			Assert.Equal(settings.Interceptors, copy.Interceptors);
			return;
		}

		if (property.Name != nameof(KurrentDBClientSettings.OperationOptions)) {
			Assert.Equal(property.GetValue(settings), property.GetValue(copy));
			return;
		}

		Assert.NotSame(settings.OperationOptions, copy.OperationOptions);
		Assert.All(
			typeof(KurrentDBClientOperationOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance),
			x => Assert.Equal(x.GetValue(settings.OperationOptions), x.GetValue(copy.OperationOptions))
		);
	}

	[Fact]
	public void setting_a_property_of_the_clone_leaves_the_original() {
		var settings = CreateSettings();
		var clone    = settings.Clone();

		clone.ConnectionName    = "other";
		clone.EnableRichSubscriptionLiveness = false;

		Assert.Equal("connection", settings.ConnectionName);
		Assert.True(settings.EnableRichSubscriptionLiveness);
	}

	[Fact]
	public void changing_the_list_of_interceptors_leaves_the_clone() {
		var interceptors = new List<Interceptor> { new TestInterceptor() };
		var settings     = new KurrentDBClientSettings { Interceptors = interceptors };
		var clone        = settings.Clone();

		interceptors.Add(new TestInterceptor());

		Assert.Same(interceptors[0], Assert.Single(clone.Interceptors!));
		Assert.Null(new KurrentDBClientSettings().Clone().Interceptors);
	}

	class TestInterceptor : Interceptor;

	[Fact]
	public async Task client_returns_a_copy_of_its_settings() {
		var settings = CreateSettings();
		settings.ConnectivitySettings = KurrentDBClientSettings.Create("kurrentdb://localhost:2113?tls=false").ConnectivitySettings;
		settings.CreateHttpMessageHandler = null;

		await using var client = new KurrentDBClient(settings);

		var copy = client.GetSettings();
		Assert.NotSame(settings, copy);
		Assert.All(Properties, property => AssertCopied(property, settings, copy));

		copy.EnableRichSubscriptionLiveness = false;
		Assert.True(client.GetSettings().EnableRichSubscriptionLiveness);
	}
}
