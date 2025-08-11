namespace Kurrent.Client.Streams;

[PublicAPI]
public static class MetadataExtensions {
    public static Metadata WithStreamName(this Metadata metadata, StreamName stream) =>
        metadata.With("Stream", stream);
}
