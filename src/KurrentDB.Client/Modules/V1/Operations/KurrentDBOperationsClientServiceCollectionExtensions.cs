// ReSharper disable CheckNamespace

using System.Net.Http;
using KurrentDB.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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
	public static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services, Uri address,
	                                                              Func<HttpMessageHandler>? createHttpMessageHandler = null)
		=> services.AddKurrentDBOperationsClient(options => {
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
	public static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services,
	                                                              Action<KurrentDBClientSettings>? configureOptions = null) =>
		services.AddKurrentDBOperationsClient(new KurrentDBClientSettings(), configureOptions);

	/// <summary>
	/// Adds an <see cref="KurrentDBOperationsClient"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="connectionString"></param>
	/// <param name="configureOptions"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services,
	                                                              string connectionString, Action<KurrentDBClientSettings>? configureOptions = null) =>
		services.AddKurrentDBOperationsClient(KurrentDBClientSettings.Create(connectionString), configureOptions);

	static IServiceCollection AddKurrentDBOperationsClient(this IServiceCollection services,
	                                                       KurrentDBClientSettings options, Action<KurrentDBClientSettings>? configureOptions) {
		if (services == null) {
			throw new ArgumentNullException(nameof(services));
		}

		configureOptions?.Invoke(options);

		services.TryAddSingleton(provider => {
			options.LoggerFactory ??= provider.GetService<ILoggerFactory>();
			options.Interceptors  ??= provider.GetServices<Interceptor>();

			return new KurrentDBOperationsClient(options);
		});

		return services;
	}
}
// ReSharper restore CheckNamespace
