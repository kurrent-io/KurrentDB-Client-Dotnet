// ReSharper disable CheckNamespace

using System.Net.Http;
using Grpc.Core.Interceptors;
using KurrentDb.Client;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection {
	/// <summary>
	/// A set of extension methods for <see cref="IServiceCollection"/> which provide support for an <see cref="KurrentDBPersistentSubscriptionsClient"/>.
	/// </summary>
	public static class KurrentDBPersistentSubscriptionsClientCollectionExtensions {
		/// <summary>
		/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			Uri address, Func<HttpMessageHandler>? createHttpMessageHandler = null)
			=> services.AddKurrentPersistentSubscriptionsClient(options => {
				options.ConnectivitySettings.Address = address;
				options.CreateHttpMessageHandler = createHttpMessageHandler;
			});

		/// <summary>
		/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			Action<KurrentDBClientSettings>? configureSettings = null) =>
			services.AddKurrentPersistentSubscriptionsClient(new KurrentDBClientSettings(),
				configureSettings);

		/// <summary>
		/// Adds an <see cref="KurrentDBPersistentSubscriptionsClient"/> to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <exception cref="ArgumentNullException"></exception>
		public static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			string connectionString, Action<KurrentDBClientSettings>? configureSettings = null) =>
			services.AddKurrentPersistentSubscriptionsClient(KurrentDBClientSettings.Create(connectionString),
				configureSettings);

		private static IServiceCollection AddKurrentPersistentSubscriptionsClient(this IServiceCollection services,
			KurrentDBClientSettings settings, Action<KurrentDBClientSettings>? configureSettings) {
			if (services == null) {
				throw new ArgumentNullException(nameof(services));
			}

			configureSettings?.Invoke(settings);
			services.TryAddSingleton(provider => {
				settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
				settings.Interceptors ??= provider.GetServices<Interceptor>();

				return new KurrentDBPersistentSubscriptionsClient(settings);
			});
			return services;
		}
	}
}
// ReSharper restore CheckNamespace
