// ReSharper disable CheckNamespace

using System.Net.Http;
using Grpc.Core.Interceptors;
using KurrentDb.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDbOperationsClient"/>.
	/// </summary>
	public static class KurrentOperationsClientServiceCollectionExtensions {

		/// <summary>
		/// Adds an <see cref="KurrentDbOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentOperationsClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentOperationsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDbOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentOperationsClient(this IServiceCollection services,
			Action<KurrentDbClientSettings>? configureOptions = null) =>
			services.AddKurrentOperationsClient(new KurrentDbClientSettings(), configureOptions);

		/// <summary>
		/// Adds an <see cref="KurrentDbOperationsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureOptions"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentOperationsClient(this IServiceCollection services,
			string connectionString, Action<KurrentDbClientSettings>? configureOptions = null) =>
			services.AddKurrentOperationsClient(KurrentDbClientSettings.Create(connectionString), configureOptions);

		private static IServiceCollection AddKurrentOperationsClient(this IServiceCollection services,
			KurrentDbClientSettings options, Action<KurrentDbClientSettings>? configureOptions) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureOptions?.Invoke(options);

			services.TryAddSingleton(provider => {
				options.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				options.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDbOperationsClient(options);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
