using System.Text;
using Kurrent.Client.Registry;
using Kurrent.Client.Schema.Serialization.Json;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Model;

public class MetadataDecoderTests {
    [Test]
    public void metadata_can_be_locked() {
        var originalMetadata = new Metadata()
            .With(SystemMetadataKeys.SchemaName, "test-schema")
            .With(SystemMetadataKeys.SchemaDataFormat, SchemaDataFormat.Protobuf)
            .With("guid-value", Guid.NewGuid())
            .With("guid-empty-value", Guid.Empty)
            .With("guid-null-value", (Guid?)null)
            .With("int-value", int.MaxValue)
            .With("int-null-value", (int?)null)
            .With("long-value", long.MaxValue)
            .With("long-null-value", (long?)null)
            .With("float-value", float.MaxValue)
            .With("float-null-value", (float?)null)
            .With("double-value", double.MaxValue)
            .With("double-null-value", (double?)null)
            .With("decimal-value", decimal.MaxValue)
            .With("decimal-null-value", (decimal?)null)
            .With("string-value", "testValue")
            .With("string-null-value", (string?)null)
            .With("bool-true-value", true)
            .With("bool-false-value", false)
            .With("bool-null-value", (bool?)null)
            .With("datetime-value", DateTime.Now)
            .With("datetime-utc-value", DateTime.UtcNow)
            .With("datetime-null-value", (DateTime?)null)
            .With("datetimeoffset-value", DateTimeOffset.Now)
            .With("datetimeoffset-utc-value", DateTimeOffset.UtcNow)
            .With("datetimeoffset-null-value", (DateTimeOffset?)null)
            .With("timespan-value", TimeSpan.FromHours(1))
            .With("timespan-null-value", (TimeSpan?)null)
            .With("bytes-value", "testBytes"u8.ToArray())
            .With("bytes-null-value", (byte[]?)null);

        var bytes = new JsonSerializer().Serialize(originalMetadata);

        var json = Encoding.UTF8.GetString(bytes.Span);

        var sut1 = new DefaultMetadataDecoder();

        var metadata1 = sut1.Decode(bytes, new MetadataDecoderContext(StreamName.None, SchemaName.None, SchemaDataFormat.Json));

        var myInt2  = metadata1.GetOrDefault<string>("int-value");
        var myDate1 = metadata1.GetOrDefault<DateTime>("datetime-value");
        var myDate2 = metadata1.GetOrDefault<DateTime>("datetime-utc-value");
    }
}
