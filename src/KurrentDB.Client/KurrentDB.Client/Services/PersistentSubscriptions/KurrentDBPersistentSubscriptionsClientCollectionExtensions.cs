// ReSharper disable CheckNamespace

using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBPersistentSubscriptionsClient"/>.
/// </summary>
public static class KurrentDBPersistentSubscriptionsClientCollectionExtensions {
	/// <summary>
	/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBPersistentSubscriptionsClient(
		this IServiceCollection services, Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null
	) =>
		services.AddKurrentDBPersistentSubscriptionsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler     = createHttpMessageHandler;
			}
		);

	/// <summary>
	/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBPersistentSubscriptionsClient(this IServiceCollection services, Action<KurrentDBClientSettings>? configureSettings = null) =>
		services.AddKurrentDBPersistentSubscriptionsClient(
			new KurrentDBClientSettings(),
			configureSettings
		);

	/// <summary>
	/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBPersistentSubscriptionsClient(
		this IServiceCollection services, string connectionString, Action<KurrentDBClientSettings>? configureSettings = null
	) =>
		services.AddKurrentDBPersistentSubscriptionsClient(
			KurrentDBClientSettings.Create(connectionString),
			configureSettings
		);

	static IServiceCollection AddKurrentDBPersistentSubscriptionsClient(
		this IServiceCollection services, KurrentDBClientSettings settings, Action<KurrentDBClientSettings>? configureSettings
	) {
		if (services is null)
			throw new ArgumentNullException(nameof(services));

		configureSettings?.Invoke(settings);
		services.TryAddSingleton(provider => {
				settings.LoggerFactory = settings.LoggerFactory == NullLoggerFactory.Instance
					? provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance
					: settings.LoggerFactory;

				return new KurrentDBPersistentSubscriptionsClient(settings);
			}
		);

		return services;
	}
}
// ReSharper restore CheckNamespace
