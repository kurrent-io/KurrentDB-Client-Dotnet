// ReSharper disable CheckNamespace

using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBUserManagementClient"/>.
/// </summary>
public static class KurrentDBUserManagementClientCollectionExtensions {
	/// <summary>
	/// Adds an <see cref="KurrentDBUserManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="address"></param>
	/// <param name="createHttpMessageHandler"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBUserManagementClient(this IServiceCollection services, Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null) =>
		services.AddKurrentDBUserManagementClient(options => {
			options.ConnectivitySettings.Address = address;
			options.CreateHttpMessageHandler     = createHttpMessageHandler;
		});

	/// <summary>
	/// Adds an <see cref="KurrentDBUserManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="connectionString"></param>
	/// <param name="configureSettings"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBUserManagementClient(this IServiceCollection services, string connectionString, Action<KurrentDBClientSettings>? configureSettings = null) =>
		services.AddKurrentDBUserManagementClient(KurrentDBClientSettings.Create(connectionString), configureSettings);

	/// <summary>
	/// Adds an <see cref="KurrentDBUserManagementClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configureSettings"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBUserManagementClient(this IServiceCollection services, Action<KurrentDBClientSettings>? configureSettings = null) =>
		services.AddKurrentDBUserManagementClient(new KurrentDBClientSettings(), configureSettings);

	static IServiceCollection AddKurrentDBUserManagementClient(this IServiceCollection services, KurrentDBClientSettings settings, Action<KurrentDBClientSettings>? configureSettings = null) {
		configureSettings?.Invoke(settings);

		services.TryAddSingleton(provider => {
				settings.LoggerFactory = settings.LoggerFactory == NullLoggerFactory.Instance
					? provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance
					: settings.LoggerFactory;

				return new KurrentDBUserManagementClient(settings);
			}
		);

		return services;
	}
}
// ReSharper restore CheckNamespace
