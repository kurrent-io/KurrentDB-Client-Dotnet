using Kurrent.Client.Streams;

namespace Kurrent.Client.Registry;

public readonly record struct SchemaDetails(
    SchemaDataFormat DataFormat,
    CompatibilityMode Compatibility,
    string Description,
    IReadOnlyDictionary<string, string> Tags
) {
    internal static SchemaDetails FromProto(KurrentDB.Protocol.Registry.V2.SchemaDetails details) {
		ArgumentNullException.ThrowIfNull(details);

		return new SchemaDetails(
            (SchemaDataFormat)details.DataFormat,
            (CompatibilityMode)details.Compatibility,
            details.Description = details.HasDescription ? details.Description : string.Empty,
            details.Tags
        );
    }

    internal KurrentDB.Protocol.Registry.V2.SchemaDetails ToProto() {
        var details = new KurrentDB.Protocol.Registry.V2.SchemaDetails {
            DataFormat    = (KurrentDB.Protocol.Registry.V2.SchemaDataFormat)DataFormat,
            Compatibility = (KurrentDB.Protocol.Registry.V2.CompatibilityMode)Compatibility,
            //Tags          = { Tags.ToDictionary(x => x.Key, x => x.Value) }
        };

        if (!string.IsNullOrEmpty(Description))
            details.Description = Description;

        foreach (var tag in Tags)
            details.Tags.Add(tag.Key, tag.Value);

        return details;
    }
}
