using System.IO.Compression;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Compression;

namespace Kurrent.Grpc.Compression;

public static class GrpcCompression {
	public const string CompressionRequestAlgorithmHeader = "grpc-internal-encoding-request";

	public static readonly Dictionary<string, ICompressionProvider> FastestCompressionProviders = new(StringComparer.OrdinalIgnoreCase) {
		["gzip"]    = new GzipCompressionProvider(CompressionLevel.Fastest),
		["deflate"] = new DeflateCompressionProvider(CompressionLevel.Fastest),
		["br"]      = new BrotliCompressionProvider(CompressionLevel.Fastest),
	};

	public static readonly Dictionary<string, ICompressionProvider> OptimalCompressionProviders = new(StringComparer.OrdinalIgnoreCase) {
		["gzip"]    = new GzipCompressionProvider(CompressionLevel.Optimal),
		["deflate"] = new DeflateCompressionProvider(CompressionLevel.Optimal),
		["br"]      = new BrotliCompressionProvider(CompressionLevel.Optimal),
	};

	public static readonly Dictionary<string, ICompressionProvider> SmallestSizeCompressionProviders = new(StringComparer.OrdinalIgnoreCase) {
		["gzip"]    = new GzipCompressionProvider(CompressionLevel.SmallestSize),
		["deflate"] = new DeflateCompressionProvider(CompressionLevel.SmallestSize),
		["br"]      = new BrotliCompressionProvider(CompressionLevel.SmallestSize),
	};

	public enum CompressionMethod {
		Gzip,
		Deflate,
		Brotli
	}

	/// <summary>
	/// Provides a collection of default gRPC compression providers, which includes
	/// providers for "gzip", "deflate", and "br" (Brotli) compression methods.
	/// </summary>
	/// <remarks>
	/// The default compression providers are initialized with the fastest compression level
	/// for optimal performance. This collection is used to enable support for these
	/// compression algorithms in gRPC communication.
	/// </remarks>
	public static readonly IList<ICompressionProvider> DefaultFastestProviders = FastestCompressionProviders.Values.ToList();

	/// <summary>
	/// Provides a collection of gRPC compression providers configured with the "Optimal" compression level,
	/// including support for "gzip", "deflate", and "br" (Brotli) compression methods.
	/// </summary>
	/// <remarks>
	/// This collection is designed for scenarios requiring a balance between compression efficiency and speed,
	/// using the "Optimal" compression level for each supported algorithm in gRPC communication.
	/// </remarks>
	public static readonly IList<ICompressionProvider> DefaultOptimalProviders = OptimalCompressionProviders.Values.ToList();

	/// <summary>
	/// Provides a collection of default gRPC compression providers configured to use
	/// the smallest size compression level. This collection includes providers for
	/// "gzip", "deflate", and "br" (Brotli) compression methods.
	/// </summary>
	/// <remarks>
	/// The smallest size compression level prioritizes achieving the smallest possible
	/// data size, often at the cost of increased processing time. This collection is
	/// typically used when minimizing data size is a higher priority than compression
	/// throughput.
	/// </remarks>
	public static readonly IList<ICompressionProvider> DefaultSmallestSizeProviders = SmallestSizeCompressionProviders.Values.ToList();

	/// <summary>
	/// Configures the specified gRPC channel options with a set of compression providers corresponding to the specified compression level.
	/// </summary>
	/// <param name="options">The gRPC channel options to which the compression providers will be added.</param>
	/// <param name="level">The desired compression level. Supported levels are Fastest, Optimal, and SmallestSize. Defaults to Optimal.</param>
	/// <returns>The updated <see cref="GrpcChannelOptions"/> with the appropriate compression providers applied.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the specified compression level is NoCompression or if an unsupported compression level is provided.</exception>
	public static GrpcChannelOptions WithCompressionProviders(this GrpcChannelOptions options, CompressionLevel level = CompressionLevel.Optimal) {
		if (level == CompressionLevel.NoCompression)
			throw new ArgumentOutOfRangeException(nameof(level), "Compression level cannot be NoCompression");

		options.CompressionProviders = level switch {
			CompressionLevel.Fastest      => DefaultFastestProviders,
			CompressionLevel.Optimal      => DefaultOptimalProviders,
			CompressionLevel.SmallestSize => DefaultSmallestSizeProviders,
			_                             => throw new ArgumentOutOfRangeException(nameof(level), "Unsupported compression level")
		};

		return options;
	}

	/// <summary>
	/// Adds the specified compression method to the gRPC call options. If a compression header
	/// with the key "grpc-internal-encoding-request" is not already present, it appends it with
	/// the selected compression method.
	/// </summary>
	/// <param name="options">The existing gRPC call options.</param>
	/// <param name="method">The compression method to apply. Defaults to Gzip if not specified.</param>
	/// <returns>The updated CallOptions with the compression header added if it was not already present.</returns>
	public static CallOptions WithCompression(this CallOptions options, CompressionMethod method = CompressionMethod.Gzip) {
		options.Headers.WithCompression(method);
		return options;
	}

	/// <summary>
	/// Removes any existing compression settings from the gRPC call options by removing the related compression headers.
	/// </summary>
	/// <param name="options">The existing gRPC call options to be modified.</param>
	/// <returns>The updated CallOptions with any compression headers removed.</returns>
	public static CallOptions WithoutCompression(this CallOptions options) {
		options.Headers.WithoutCompression();
		return options;
	}

	/// <summary>
	/// Adds the specified compression method to the gRPC call options. If a compression header
	/// with the key "grpc-internal-encoding-request" is not already present, it appends it with
	/// the selected compression method.
	/// </summary>
	/// <param name="headers">The existing headers to which the compression header will be added.</param>
	/// <param name="method">The compression method to apply. Defaults to Gzip if not specified.</param>
	/// <returns>The updated headers with the compression header added if it was not already present.</returns>
	public static Metadata? WithCompression(this Metadata? headers, CompressionMethod method = CompressionMethod.Gzip) {
		if (headers is null)
			return headers;

		var hasCompressionHeader = headers.Any(m =>
			string.Equals(m.Key, CompressionRequestAlgorithmHeader, StringComparison.OrdinalIgnoreCase)
		);

		if (!hasCompressionHeader)
			headers.Add(CompressionRequestAlgorithmHeader, method.ToString().ToLowerInvariant());

		return headers;
	}

	/// <summary>
	/// Removes the compression header ("grpc-internal-encoding-request") from the provided metadata,
	/// if present, effectively disabling compression for the associated gRPC request.
	/// </summary>
	/// <param name="headers">The existing metadata headers from which the compression header will be removed. Can be null.</param>
	/// <returns>The modified metadata without the compression header, or the original metadata if the header was not present or the input was null.</returns>
	public static Metadata? WithoutCompression(this Metadata? headers) {
		if (headers is null)
			return headers;

		var compressionHeader = headers.FirstOrDefault(x =>
			string.Equals(x.Key, CompressionRequestAlgorithmHeader, StringComparison.OrdinalIgnoreCase)
		);

		if (compressionHeader?.Key is not null)
			headers.Remove(compressionHeader);

		return headers;
	}
}
