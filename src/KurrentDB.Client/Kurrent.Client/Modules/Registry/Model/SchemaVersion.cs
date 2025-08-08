using Kurrent.Client.Streams;

namespace Kurrent.Client.Registry;

public readonly record struct SchemaVersion(
    SchemaVersionId VersionId,
    int VersionNumber,
    string SchemaDefinition,
    SchemaDataFormat DataFormat,
    DateTimeOffset RegisteredAt
) {
    internal static SchemaVersion FromProto(KurrentDB.Protocol.Registry.V2.SchemaVersion version) {
        ArgumentNullException.ThrowIfNull(version);

        return new SchemaVersion(
            SchemaVersionId.From(version.SchemaVersionId),
            version.VersionNumber,
            version.SchemaDefinition.ToStringUtf8(),
            (SchemaDataFormat)version.DataFormat,
            version.RegisteredAt.ToDateTimeOffset()
        );
    }
}
