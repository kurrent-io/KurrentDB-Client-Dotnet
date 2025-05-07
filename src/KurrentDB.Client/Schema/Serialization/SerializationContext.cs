using JetBrains.Annotations;
using KurrentDB.Client.Model;

namespace KurrentDB.Client.Schema.Serialization;

/// <summary>
/// Allows the serialization operations to be autonomous.
/// </summary>
[PublicAPI]
public readonly record struct SerializationContext {
	public SerializationContext(Metadata metadata, string stream, CancellationToken cancellationToken = default) {
		Metadata          = metadata;
		Stream            = stream;
		CancellationToken = cancellationToken;
		SchemaInfo        = SchemaInfo.FromMetadata(Metadata);
	}

	/// <summary>
    /// The metadata present in the record.
    /// </summary>
    public Metadata Metadata { get; }

    /// <summary>
    /// The stream that the record belongs to.
    /// </summary>
    public string Stream { get; }

    /// <summary>
    /// The token used to propagate cancellation notifications in the serialization context.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// The schema information extracted from the headers.
    /// If the headers do not contain schema information, it will return an undefined schema information.
    /// </summary>
    public SchemaInfo SchemaInfo { get; }

    // /// <summary>
    // /// Creates a new instance of the <see cref="SerializationContext"/> record struct with the provided headers.
    // /// </summary>
    // /// <param name="headers">The headers to be included in the serialization context.</param>
    // public static SerializationContext From(Metadata headers) => new(headers);

    // /// <summary>
    // /// Creates a new instance of the <see cref="SerializationContext"/> record struct with the provided schema information.
    // /// The schema information is added to a new instance of the <see cref="Metadata"/> class which is then used to create the serialization context.
    // /// </summary>
    // /// <param name="schemaInfo">The schema information to be included in the serialization context.</param>
    // public static SerializationContext From(SchemaInfo schemaInfo) =>
    //     From(new Metadata().WithSchemaInfo(schemaInfo));
    public void Deconstruct(out Metadata metadata, out string stream, out CancellationToken cancellationToken) {
	    metadata          = Metadata;
	    stream            = Stream;
	    cancellationToken = CancellationToken;
    }
}
