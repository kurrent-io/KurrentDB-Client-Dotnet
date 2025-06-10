// ReSharper disable CheckNamespace

using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBOperationsClient"/>.
/// </summary>
public static class KurrentDBOperationsClientServiceCollectionExtensions {
	/// <summary>
	/// Adds an <see cref="KurrentDBOperationsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="address"></param>
	/// <param name="createHttpMessageHandler"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services, Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null) =>
		services.AddKurrentDBOperationsClient(options => {
			options.ConnectivitySettings.Address = address;
			options.CreateHttpMessageHandler     = createHttpMessageHandler;
		});

	/// <summary>
	/// Adds an <see cref="KurrentDBOperationsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="configureOptions"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services, Action<KurrentDBClientSettings>? configureOptions = null) =>
		services.AddKurrentDBOperationsClient(new KurrentDBClientSettings(), configureOptions);

	/// <summary>
	/// Adds an <see cref="KurrentDBOperationsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="connectionString"></param>
	/// <param name="configureOptions"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBOperationsClient(
		this IServiceCollection services, string connectionString, Action<KurrentDBClientSettings>? configureOptions = null
	) =>
		services.AddKurrentDBOperationsClient(KurrentDBClientSettings.Create(connectionString), configureOptions);

	static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services, KurrentDBClientSettings options, Action<KurrentDBClientSettings>? configureOptions) {
		configureOptions?.Invoke(options);

		services.TryAddSingleton(provider => {
			options.LoggerFactory = options.LoggerFactory == NullLoggerFactory.Instance
				? provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance
				: options.LoggerFactory;

			return new KurrentDBOperationsClient(options);
		});

		return services;
	}
}
// ReSharper restore CheckNamespace
