// ReSharper disable CheckNamespace

using System.Net.Http;
using KurrentDb.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDbUserManagementClient"/>.
	/// </summary>
	public static class KurrentDbUserManagementClientCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentDbUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentUserManagementClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDbUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			string connectionString, Action<KurrentDbClientSettings>? configureSettings = null)
			=> services.AddKurrentUserManagementClient(KurrentDbClientSettings.Create(connectionString),
				configureSettings);


		/// <summary>
		/// Adds an <see cref="KurrentDbUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			Action<KurrentDbClientSettings>? configureSettings = null) =>
			services.AddKurrentUserManagementClient(new KurrentDbClientSettings(), configureSettings);

		private static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			KurrentDbClientSettings settings, Action<KurrentDbClientSettings>? configureSettings = null) {
			configureSettings?.Invoke(settings);
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDbUserManagementClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
