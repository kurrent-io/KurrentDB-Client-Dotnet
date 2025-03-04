// ReSharper disable CheckNamespace

using System.Net.Http;
using KurrentDB.Client;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
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
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentUserManagementClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDBUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			string connectionString, Action<KurrentDBClientSettings>? configureSettings = null)
			=> services.AddKurrentUserManagementClient(KurrentDBClientSettings.Create(connectionString),
				configureSettings);


		/// <summary>
		/// Adds an <see cref="KurrentDBUserManagementClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			Action<KurrentDBClientSettings>? configureSettings = null) =>
			services.AddKurrentUserManagementClient(new KurrentDBClientSettings(), configureSettings);

		private static IServiceCollection AddKurrentUserManagementClient(this IServiceCollection services,
			KurrentDBClientSettings settings, Action<KurrentDBClientSettings>? configureSettings = null) {
			configureSettings?.Invoke(settings);
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBUserManagementClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
