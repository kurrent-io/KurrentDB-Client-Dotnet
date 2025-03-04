// ReSharper disable CheckNamespace

using System.Net.Http;
using Grpc.Core.Interceptors;
using KurrentDb.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBClient"/>.
	/// </summary>
	public static class KurrentDBClientServiceCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="address"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services, Uri address,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentDbClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="addressFactory"></param>
		/// <param name="createHttpMessageHandler"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, Uri> addressFactory,
			Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentDbClient(provider => options => {
				options.ConnectivitySettings.Address = addressFactory(provider);
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Action<KurrentDBClientSettings>? configureSettings = null) =>
			services.AddKurrentDbClient(new KurrentDBClientSettings(), configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, Action<KurrentDBClientSettings>> configureSettings) =>
			services.AddKurrentDbClient(new KurrentDBClientSettings(),
				configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionString"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			string connectionString, Action<KurrentDBClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentDbClient(KurrentDBClientSettings.Create(connectionString), configureSettings);
		}

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="connectionStringFactory"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, string> connectionStringFactory,
			Action<KurrentDBClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentDbClient(provider => KurrentDBClientSettings.Create(connectionStringFactory(provider)), configureSettings);
		}

		private static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			KurrentDBClientSettings settings,
			Action<KurrentDBClientSettings>? configureSettings) {
			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBClient(settings);
			});

			return services;
		}

		private static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, KurrentDBClientSettings> settingsFactory,
			Action<KurrentDBClientSettings>? configureSettings = null) {

			services.TryAddSingleton(provider => {
				var settings = settingsFactory(provider);
				configureSettings?.Invoke(settings);

				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBClient(settings);
			});

			return services;
		}

		private static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			KurrentDBClientSettings settings,
			Func<IServiceProvider, Action<KurrentDBClientSettings>> configureSettingsFactory) {

			services.TryAddSingleton(provider => {
				configureSettingsFactory(provider).Invoke(settings);

				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBClient(settings);
			});

			return services;
		}
	}
}
// ReSharper restore CheckNamespace
