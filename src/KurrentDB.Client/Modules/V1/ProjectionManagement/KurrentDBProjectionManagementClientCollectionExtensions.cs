// ReSharper disable CheckNamespace

using System.Net.Http;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBProjectionManagementClient"/>.
/// </summary>
public static class KurrentDBProjectionManagementClientCollectionExtensions {
	/// <summary>
	/// Adds an <see cref="KurrentDBProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="address"></param>
	/// <param name="createHttpMessageHandler"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBProjectionManagementClient(this IServiceCollection services, Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null) =>
		services.AddKurrentDBProjectionManagementClient(options => {
			options.ConnectivitySettings.Address = address;
			options.CreateHttpMessageHandler     = createHttpMessageHandler;
		});

	/// <summary>
	/// Adds an <see cref="KurrentDBProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configureSettings"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBProjectionManagementClient(this IServiceCollection services, Action<KurrentDBClientSettings>? configureSettings = null) =>
		services.AddKurrentDBProjectionManagementClient(new KurrentDBClientSettings(), configureSettings);

	/// <summary>
	/// Adds an <see cref="KurrentDBProjectionManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="connectionString"></param>
	/// <param name="configureSettings"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBProjectionManagementClient(this IServiceCollection services, string connectionString, Action<KurrentDBClientSettings>? configureSettings = null) =>
		services.AddKurrentDBProjectionManagementClient(KurrentDBClientSettings.Create(connectionString), configureSettings);

	static IServiceCollection AddKurrentDBProjectionManagementClient(this IServiceCollection services, KurrentDBClientSettings settings, Action<KurrentDBClientSettings>? configureSettings) {
		configureSettings?.Invoke(settings);

		services.TryAddSingleton(provider => {
			settings.LoggerFactory = settings.LoggerFactory == NullLoggerFactory.Instance
				? provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance
				: settings.LoggerFactory;

			return new KurrentDBProjectionManagementClient(settings);
		});

		return services;
	}
}
// ReSharper restore CheckNamespace
