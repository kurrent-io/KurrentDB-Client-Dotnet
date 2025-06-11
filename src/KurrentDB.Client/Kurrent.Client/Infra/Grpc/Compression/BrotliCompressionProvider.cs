using System.IO.Compression;
using Grpc.Net.Compression;

namespace Kurrent.Client.Grpc.Compression;

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
