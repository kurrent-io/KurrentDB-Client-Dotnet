using Grpc.Core;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KurrentDB.Client;

/// <summary>
/// Extension methods for registering the legacy call invoker with dependency injection.
/// </summary>
public static class KurrentDBLegacyExtensions {
	/// <summary>
	/// Adds a gRPC client that uses the LegacyClusterClient for channel management.
	/// </summary>
	/// <typeparam name="TClient">The type of gRPC client to add.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to add the client to.</param>
	/// <param name="configureClient">An optional action to configure the client.</param>
	/// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
	/// <remarks>
	/// This method registers a gRPC client that uses the LegacyCallInvoker to delegate to the LegacyClusterClient.
	/// The LegacyClusterClient must be registered in the service collection before calling this method.
	/// </remarks>
	public static IServiceCollection AddLegacyGrpcClient<TClient>(this IServiceCollection services, Action<GrpcClientFactoryOptions>? configureClient = null) where TClient : class {
		services.AddHttpClient();

		// Register the LegacyCallInvoker as a singleton
		services.TryAddSingleton<KurrentDBLegacyCallInvoker>(provider => {
				var legacyClient = provider.GetRequiredService<LegacyClusterClient>();
				return new KurrentDBLegacyCallInvoker(legacyClient);
			}
		);

		// Register the gRPC client
		var builder = services.AddGrpcClient<TClient>(options => {
				// Set the address to something valid (it won't be used, but gRPC client factory requires it)
				options.Address = new Uri("http://placeholder");

				// Apply additional configuration if provided
				configureClient?.Invoke(options);
			}
		);

		// Configure the client creator to use our LegacyCallInvoker
		builder.ConfigureGrpcClientCreator((serviceProvider, defaultCallInvoker) => {
				// Get our LegacyCallInvoker
				var legacyInvoker = serviceProvider.GetRequiredService<KurrentDBLegacyCallInvoker>();

				// Create the gRPC client with our invoker
				var constructorInfo = typeof(TClient).GetConstructor([typeof(CallInvoker)]);
				if (constructorInfo == null)
					throw new InvalidOperationException(
						$"No suitable constructor found for {typeof(TClient).Name}. " +
						$"The client must have a constructor that accepts a CallInvoker parameter."
					);

				return (TClient)constructorInfo.Invoke([legacyInvoker]);
			}
		);

		return services;
	}

	/// <summary>
	/// Gets the current server capabilities from the LegacyCallInvoker.
	/// </summary>
	/// <param name="serviceProvider">The service provider.</param>
	/// <returns>The current server capabilities.</returns>
	/// <exception cref="InvalidOperationException">Thrown if LegacyCallInvoker is not registered.</exception>
	public static ServerCapabilities GetServerCapabilities(this IServiceProvider serviceProvider) {
		var invoker = serviceProvider.GetService<KurrentDBLegacyCallInvoker>();
		if (invoker == null)
			throw new InvalidOperationException(
				"LegacyCallInvoker is not registered. " +
				"Use AddLegacyGrpcClient<TClient>() to register it."
			);

		return invoker.ServerCapabilities;
	}
}
