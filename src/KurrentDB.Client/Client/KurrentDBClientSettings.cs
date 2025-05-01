using System;
using System.Collections.Generic;
using System.Net.Http;
using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.Logging;

namespace KurrentDB.Client;

/// <summary>
/// A class that represents the settings to use for operations made from an implementation of <see cref="KurrentDBClientBase"/>.
/// </summary>
public partial class KurrentDBClientSettings {
	/// <summary>
	/// An optional list of <see cref="Interceptor"/>s to use.
	/// </summary>
	public IEnumerable<Interceptor>? Interceptors { get; set; }

	/// <summary>
	/// The name of the connection.
	/// </summary>
	public string? ConnectionName { get; set; }

	/// <summary>
	/// An optional <see cref="HttpMessageHandler"/> factory.
	/// </summary>
	public Func<HttpMessageHandler>? CreateHttpMessageHandler { get; set; }

	/// <summary>
	/// An optional <see cref="ILoggerFactory"/> to use.
	/// </summary>
	public ILoggerFactory? LoggerFactory { get; set; }

	/// <summary>
	/// The optional <see cref="ChannelCredentials"/> to use when creating the <see cref="ChannelBase"/>.
	/// </summary>
	public ChannelCredentials? ChannelCredentials { get; set; }

	/// <summary>
	/// The default <see cref="KurrentDBClientOperationOptions"/> to use.
	/// </summary>
	public KurrentDBClientOperationOptions OperationOptions { get; set; } =
		KurrentDBClientOperationOptions.Default;

	/// <summary>
	/// The <see cref="KurrentDBClientConnectivitySettings"/> to use.
	/// </summary>
	public KurrentDBClientConnectivitySettings ConnectivitySettings { get; set; } =
		KurrentDBClientConnectivitySettings.Default;

	/// <summary>
	/// The optional <see cref="UserCredentials"/> to use if none have been supplied to the operation.
	/// </summary>
	public UserCredentials? DefaultCredentials { get; set; }

	/// <summary>
	/// The default deadline for calls. Will not be applied to reads or subscriptions.
	/// </summary>
	public TimeSpan? DefaultDeadline { get; set; } = TimeSpan.FromSeconds(10);
}