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
			Action<KurrentDbClientSettings>? configureSettings = null) =>
			services.AddKurrentDbClient(new KurrentDbClientSettings(), configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentDBClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services"></param>
		/// <param name="configureSettings"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, Action<KurrentDbClientSettings>> configureSettings) =>
			services.AddKurrentDbClient(new KurrentDbClientSettings(),
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
			string connectionString, Action<KurrentDbClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentDbClient(KurrentDbClientSettings.Create(connectionString), configureSettings);
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
			Action<KurrentDbClientSettings>? configureSettings = null) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			return services.AddKurrentDbClient(provider => KurrentDbClientSettings.Create(connectionStringFactory(provider)), configureSettings);
		}

		private static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			KurrentDbClientSettings settings,
			Action<KurrentDbClientSettings>? configureSettings) {
			configureSettings?.Invoke(settings);

			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBClient(settings);
			});

			return services;
		}

		private static IServiceCollection AddKurrentDbClient(this IServiceCollection services,
			Func<IServiceProvider, KurrentDbClientSettings> settingsFactory,
			Action<KurrentDbClientSettings>? configureSettings = null) {

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
			KurrentDbClientSettings settings,
			Func<IServiceProvider, Action<KurrentDbClientSettings>> configureSettingsFactory) {

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
