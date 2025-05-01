using JetBrains.Annotations;

namespace KurrentDB.Client.Model;

[PublicAPI]
public readonly record struct Message() {
	public static readonly Message Empty = new();

	/// <summary>
	/// The message payload.
	/// </summary>
	public object Value { get; init; } = null!;

	/// <summary>
	/// The message metadata.
	/// </summary>
	public Metadata Metadata { get; init; } = new Metadata {
        [SystemMetadataKeys.SchemaDataFormat] = nameof(SchemaDataFormat.Json).ToLower()
    };

	/// <summary>
	/// The assigned record id.
	/// </summary>
	public Guid RecordId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The schema info of the message.
    /// </summary>
    public SchemaInfo Schema { get; init; } = new("", SchemaDataFormat.Json);
}
