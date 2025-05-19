using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Grpc.Net.Client;
using Grpc.Net.Compression;
using EndPoint = System.Net.EndPoint;

namespace KurrentDB.Client;

#if NET6_0_OR_GREATER
/// <summary>
/// Provides Brotli compression and decompression functionality
/// for gRPC communication. Implements the <see cref="ICompressionProvider"/> interface.
/// </summary>
/// <param name="defaultCompressionLevel">The default compression level to use when compressing data.</param>
public class BrotliCompressionProvider(CompressionLevel? defaultCompressionLevel = null) : ICompressionProvider {
	readonly CompressionLevel _defaultCompressionLevel = defaultCompressionLevel ?? CompressionLevel.Fastest;

	public string EncodingName => "br";

	public Stream CreateCompressionStream(Stream stream, CompressionLevel? compressionLevel) =>
		new BrotliStream(stream, compressionLevel ?? _defaultCompressionLevel, true);

	public Stream CreateDecompressionStream(Stream stream) =>
		new BrotliStream(stream, CompressionMode.Decompress);
}
#endif

static class ChannelFactoryExtensions {
	const int MaxReceiveMessageLength = 17 * 1024 * 1024; // 17MB

	internal static readonly Dictionary<string, ICompressionProvider> CompressionProviders = new Dictionary<string, ICompressionProvider>(StringComparer.Ordinal) {
		["gzip"] = new GzipCompressionProvider(CompressionLevel.Fastest),
#if NET8_0_OR_GREATER
		["deflate"] = new DeflateCompressionProvider(CompressionLevel.Fastest),
		["br"] = new BrotliCompressionProvider(CompressionLevel.Fastest),
#endif
	};

	static readonly IList<ICompressionProvider> DefaultCompressionProviders = CompressionProviders.Values.ToList();

	public static GrpcChannel CreateChannel(this KurrentDBClientSettings settings, EndPoint endPoint) {
		if (settings.ConnectivitySettings.Insecure) {
			//this must be switched on before creation of the HttpMessageHandler
			AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
		}

		var options = new GrpcChannelOptions {
			CompressionProviders = DefaultCompressionProviders,

			ServiceConfig = settings.RetrySettings.IsEnabled
				? new() { MethodConfigs = { settings.RetrySettings.GetRetryMethodConfig() } }
				: null,
#if NET48
			HttpHandler = CreateHandler(settings),
#else
			HttpClient = new HttpClient(CreateHandler(settings), true) {
				Timeout = Timeout.InfiniteTimeSpan,
				DefaultRequestVersion = new Version(2, 0)
			},
#endif
			LoggerFactory         = settings.LoggerFactory,
			Credentials           = settings.ChannelCredentials,
			DisposeHttpClient     = true,
			MaxReceiveMessageSize = MaxReceiveMessageLength
		};

		var address = endPoint.ToUri(!settings.ConnectivitySettings.Insecure);

		return GrpcChannel.ForAddress(address, options);


#if NET48
		static HttpMessageHandler CreateHandler(KurrentDBClientSettings settings) {
			if (settings.CreateHttpMessageHandler is not null)
				return settings.CreateHttpMessageHandler.Invoke();

			var handler = new WinHttpHandler {
				TcpKeepAliveEnabled            = true,
				TcpKeepAliveTime               = settings.ConnectivitySettings.KeepAliveTimeout,
				TcpKeepAliveInterval           = settings.ConnectivitySettings.KeepAliveInterval,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure) return handler;

			if (settings.ConnectivitySettings.ClientCertificate is not null)
				handler.ClientCertificates.Add(settings.ConnectivitySettings.ClientCertificate);

			handler.ServerCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
				false => delegate { return true; },
				true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
					if (chain is null) return false;

					chain.ChainPolicy.ExtraStore.Add(settings.ConnectivitySettings.TlsCaFile);
					return chain.Build(certificate);
				},
				_ => null
			};

			return handler;
		}
#else
		static HttpMessageHandler CreateHandler(KurrentDBClientSettings settings) {
			if (settings.CreateHttpMessageHandler is not null)
				return settings.CreateHttpMessageHandler.Invoke();

			var handler = new SocketsHttpHandler {
				KeepAlivePingDelay             = settings.ConnectivitySettings.KeepAliveInterval,
				KeepAlivePingTimeout           = settings.ConnectivitySettings.KeepAliveTimeout,
				EnableMultipleHttp2Connections = true
			};

			if (settings.ConnectivitySettings.Insecure)
				return handler;

			if (settings.ConnectivitySettings.ClientCertificate is not null) {
				handler.SslOptions.ClientCertificates = new X509CertificateCollection {
					settings.ConnectivitySettings.ClientCertificate
				};
			}

			handler.SslOptions.RemoteCertificateValidationCallback = settings.ConnectivitySettings.TlsVerifyCert switch {
				false => delegate { return true; },
				true when settings.ConnectivitySettings.TlsCaFile is not null => (sender, certificate, chain, errors) => {
					if (certificate is not X509Certificate2 peerCertificate || chain is null) return false;

					chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
					chain.ChainPolicy.CustomTrustStore.Add(settings.ConnectivitySettings.TlsCaFile);
					return chain.Build(peerCertificate);
				},
				_ => null
			};

			return handler;
		}
#endif
	}
}
