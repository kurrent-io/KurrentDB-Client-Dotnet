using Grpc.Core;
using Kurrent.Client.Features;

namespace Kurrent.Client;

/// <summary>
/// This proxy of sorts is a necessary evil to allow us to use the existing gossip implementation
/// until we can use the new one based on GRPC Load Balancing.
/// <remarks>
/// Even after the new implementation is in place, this proxy will still work perfectly
/// </remarks>
/// </summary>
/// <typeparam name="T">
/// The type of the client, which must inherit from <see cref="ClientBase{T}"/>.
/// </typeparam>
class ServiceProxy<T> where T : ClientBase<T> {
#if NET9_0_OR_GREATER
	readonly Lock _connectionLocker = new();
#else
	readonly object _connectionLocker = new();
#endif

	ServiceProxy(Func<CancellationToken, ValueTask<ConnectionInfo>> connect) => Connect = connect;

	Func<CancellationToken, ValueTask<ConnectionInfo>> Connect { get; }

	ConnectionInfo Proxy {
		get {
			lock (_connectionLocker) {
				var connectTask = Connect(CancellationToken.None);
				return connectTask.IsCompleted
					? connectTask.Result
					: connectTask.AsTask().GetAwaiter().GetResult();
			}
		}
	}

	public virtual T           ServiceClient => Proxy.ServiceClient;
	public virtual ServerInfo  ServerInfo    => Proxy.ServerInfo;
	public virtual CallInvoker CallInvoker   => Proxy.CallInvoker;

	/// <summary>
	/// Creates a new instance of <see cref="ServiceProxy{T}"/> using the specified asynchronous connection function.
	/// </summary>
	/// <param name="connect">
	/// A function that asynchronously establishes a gRPC connection and returns a <see cref="CallInvoker"/> instance and server information.
	/// </param>
	/// <returns>
	/// A new instance of <see cref="ServiceProxy{T}"/>
	/// </returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown when the connection function cannot be executed or if the connection cannot be established.
	/// </exception>
	public static ServiceProxy<T> Create(Func<CancellationToken, ValueTask<(CallInvoker CallInvoker, ServerInfo ServerInfo)>> connect) =>
		new ServiceProxy<T>(async ct => {
			try {
				var (invoker, serverInfo) = await connect(ct).ConfigureAwait(false);
				var client = (T)Activator.CreateInstance(typeof(T), invoker)!;
				return new(client, invoker, serverInfo);
			}
			catch (Exception ex) {
				throw new InvalidOperationException("Failed to connect to the server using the provided connect delegate.", ex);
			}
		});

	public static ServiceProxy<T> Create(CallInvoker callInvoker, ServerInfo serverInfo) =>
		new DirectServiceProxy(callInvoker, serverInfo);

	public static ServiceProxy<T> Create(T serviceClient, CallInvoker callInvoker, ServerInfo serverInfo) =>
		new DirectServiceProxy(serviceClient, callInvoker, serverInfo);

	internal record ConnectionInfo(T ServiceClient, CallInvoker CallInvoker, ServerInfo ServerInfo);

	class DirectServiceProxy : ServiceProxy<T> {
		public DirectServiceProxy(CallInvoker callInvoker, ServerInfo serverInfo)
			: this((T)Activator.CreateInstance(typeof(T), callInvoker)!, callInvoker, serverInfo) { }

		public DirectServiceProxy(T serviceClient, CallInvoker callInvoker, ServerInfo serverInfo)
			: base(ct => new ValueTask<ConnectionInfo>(new ConnectionInfo(serviceClient, callInvoker, serverInfo))) {
			_serviceClient = serviceClient;
			_callInvoker   = callInvoker;
			_serverInfo    = new ServerInfo();
		}

		readonly T           _serviceClient;
		readonly CallInvoker _callInvoker;
		readonly ServerInfo  _serverInfo;

		public override T           ServiceClient => _serviceClient;
		public override ServerInfo  ServerInfo    => _serverInfo;
		public override CallInvoker CallInvoker   => _callInvoker;
	}
}
