using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using KurrentDB.Client.Model;
using NJsonSchema;
using OneOf;
using Contracts = KurrentDB.Protocol.Registry.V2;
using Enum = System.Enum;

namespace KurrentDB.Client.SchemaRegistry;

public readonly record struct Schema(
	SchemaName SchemaName,
	SchemaDetails Details,
	int LatestSchemaVersion,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt
) {
	internal static Schema FromProto(Contracts.Schema schema) {
		if (schema is null) throw new ArgumentNullException(nameof(schema));

		return new Schema(
			SchemaName.From(schema.SchemaName),
			SchemaDetails.FromProto(schema.Details),
			schema.LatestSchemaVersion,
			schema.CreatedAt.ToDateTimeOffset(),
			schema.UpdatedAt.ToDateTimeOffset()
		);
	}

	internal Contracts.Schema ToProto() =>
		new() {
			SchemaName          = SchemaName,
			Details             = Details.ToProto(),
			LatestSchemaVersion = LatestSchemaVersion,
			CreatedAt           = Timestamp.FromDateTimeOffset(CreatedAt),
			UpdatedAt           = Timestamp.FromDateTimeOffset(UpdatedAt)
		};
}

public readonly record struct SchemaDetails(
	SchemaDataFormat DataFormat,
	CompatibilityMode Compatibility,
	string Description,
	IReadOnlyDictionary<string, string> Tags
) {
	internal static SchemaDetails FromProto(Contracts.SchemaDetails details) {
		if (details == null) throw new ArgumentNullException(nameof(details));

		return new SchemaDetails(
			(SchemaDataFormat)details.DataFormat,
			(CompatibilityMode)details.Compatibility,
			details.Description = details.HasDescription ? details.Description : string.Empty,
			details.Tags
		);
	}

	internal Contracts.SchemaDetails ToProto() {
		var details = new Contracts.SchemaDetails {
			DataFormat    = (Contracts.SchemaDataFormat)DataFormat,
			Compatibility = (Contracts.CompatibilityMode)Compatibility,
			//Tags          = { Tags.ToDictionary(x => x.Key, x => x.Value) }
		};

		if (!string.IsNullOrEmpty(Description))
			details.Description = Description;

		foreach (var tag in Tags)
			details.Tags.Add(tag.Key, tag.Value);

		return details;
	}
}

public readonly record struct SchemaVersionDescriptor(SchemaVersionId VersionId, int VersionNumber) {
	public static readonly SchemaVersionDescriptor None = new(SchemaVersionId.None, 0);
};

public readonly record struct SchemaVersion(
	SchemaVersionId VersionId,
	int VersionNumber,
	string SchemaDefinition,
	SchemaDataFormat DataFormat,
	DateTimeOffset RegisteredAt
) {
	internal static SchemaVersion FromProto(Contracts.SchemaVersion version) {
		if (version is null) throw new ArgumentNullException(nameof(version));

		return new SchemaVersion(
			SchemaVersionId.From(version.SchemaVersionId),
			version.VersionNumber,
			version.SchemaDefinition.ToStringUtf8(),
			(SchemaDataFormat)version.DataFormat,
			version.RegisteredAt.ToDateTimeOffset()
		);
	}
}

public readonly record struct SchemaCompatibilityError() {
	public SchemaCompatibilityErrorKind Kind    { get; init; } = SchemaCompatibilityErrorKind.Unspecified;
	public string                       Details { get; init; } = string.Empty;

	public string         PropertyPath { get; init; } = string.Empty;
	public JsonObjectType OriginalType { get; init; } = JsonObjectType.None;
	public JsonObjectType NewType      { get; init; } = JsonObjectType.None;

	public override string ToString() =>
		$"{Kind} at '{PropertyPath}': {Details}";

	internal static SchemaCompatibilityError FromProto(Contracts.SchemaCompatibilityError error) {
		if (error is null) throw new ArgumentNullException(nameof(error));

		return new SchemaCompatibilityError {
			Kind         = (SchemaCompatibilityErrorKind)error.Kind,
			Details      = error.Details,
			PropertyPath = error.PropertyPath,
			OriginalType = Enum.TryParse<JsonObjectType>(error.OriginalType, out var originalType) ? originalType : JsonObjectType.None,
			NewType      = Enum.TryParse<JsonObjectType>(error.NewType, out var newType) ? newType : JsonObjectType.None
		};
	}
}

public readonly record struct SchemaCompatibilityErrors() : IEnumerable<SchemaCompatibilityError> {
	public IReadOnlyList<SchemaCompatibilityError> Errors { get; init; } = [];

	public static SchemaCompatibilityErrors From(params SchemaCompatibilityError[] errors) =>
		new() { Errors = errors };

	public static implicit operator SchemaCompatibilityErrors(SchemaCompatibilityError[] errors) =>
		new() { Errors = errors };

	internal static SchemaCompatibilityErrors FromProto(RepeatedField<Contracts.SchemaCompatibilityError> errors) =>
		new() { Errors = errors.Select(SchemaCompatibilityError.FromProto).ToArray() };

	public IEnumerator<SchemaCompatibilityError> GetEnumerator() => Errors.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public enum SchemaCompatibilityErrorKind {
	Unspecified,
	MissingRequiredProperty,  // Backward compatibility: Required property from old schema missing in new schema
	IncompatibleTypeChange,   // Backward compatibility: Property type changed incompatibly
	OptionalToRequired,       // Backward compatibility: Property changed from optional to required
	NewRequiredProperty,      // Forward compatibility: New required property added
	RemovedProperty,          // Forward compatibility: Property removed from schema
	ArrayTypeIncompatibility, // Issues with array item types
}

public readonly record struct SchemaVersionId(Guid Value) {
	public static readonly SchemaVersionId None = new(Guid.Empty);

	public Guid Value { get; } = Value;

	public override string ToString() => Value.ToString("D");

	public static SchemaVersionId From(Guid value) {
		return value == Guid.Empty
			? throw new ArgumentException($"SchemaVersionId '{value}' is not a valid identifier", nameof(value))
			: new(value);
	}

	public static SchemaVersionId From(string value) {
		return !Guid.TryParse(value, out var guid)
			? throw new ArgumentException($"SchemaVersionId '{value}' is not valid.", nameof(value))
			: From(guid);
	}

	public static implicit operator SchemaVersionId(Guid _)    => From(_);
	public static implicit operator Guid(SchemaVersionId id)   => id.Value;

	public static implicit operator SchemaVersionId(string _)  => From(_);
	public static implicit operator string(SchemaVersionId id) => id.ToString();
}

public readonly record struct SchemaName(string Value) {
	public static readonly SchemaName None = new("");

	public string Value { get; } = Value;

	public override string ToString() => Value;

	public static SchemaName From(string value) {
		return string.IsNullOrWhiteSpace(value)
			? throw new ArgumentException($"SchemaName '{value}' is not valid.", nameof(value))
			: new(value);
	}

	public static implicit operator SchemaName(string _) => From(_);
	public static implicit operator string(SchemaName id) => id.ToString();
}

public enum CompatibilityMode {
	/// <summary>
	/// Default value, should not be used.
	/// </summary>
	[Description("COMPATIBILITY_MODE_UNSPECIFIED")] Unspecified = 0,
	/// <summary>
	/// Backward compatibility allows new schemas to be used with data written by previous schemas.
	/// Example: If schema version 1 has a field "name" and schema version 2 adds a new field "age",
	/// data written with schema version 1 can still be read using schema version 2.
	/// Example of invalid schema: If schema version 1 has a field "name" and schema version 2 removes the "name" field,
	/// data written with schema version 1 cannot be read using schema version 2.
	/// </summary>
	[Description("COMPATIBILITY_MODE_BACKWARD")] Backward = 1,
	/// <summary>
	/// Forward compatibility allows data written by new schemas to be read by previous schemas.
	/// Example: If schema version 1 has a field "name" and schema version 2 adds a new field "age",
	/// data written with schema version 2 can still be read using schema version 1, ignoring the "age" field.
	/// Example of invalid schema: If schema version 1 has a field "name" and schema version 2 changes the "name" field type,
	/// data written with schema version 2 cannot be read using schema version 1.
	/// </summary>
	[Description("COMPATIBILITY_MODE_FORWARD")] Forward = 2,
	/// <summary>
	/// Full compatibility ensures both backward and forward compatibility.
	/// This mode guarantees that new schemas can read data written by old schemas,
	/// and old schemas can read data written by new schemas.
	/// </summary>
	[Description("COMPATIBILITY_MODE_FULL")] Full = 3,
	/// <summary>
	/// Disables compatibility checks, allowing any kind of schema change.
	/// This mode should be used with caution, as it may lead to compatibility issues.
	/// </summary>
	[Description("COMPATIBILITY_MODE_NONE")] None = 4,
}

[PublicAPI]
public static class ErrorDetails {
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct SchemaNotFound {
		public static readonly SchemaNotFound Value = new();
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public readonly struct SchemaAlreadyExists {
		public static readonly SchemaAlreadyExists Value = new();
	}
}

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Success {
	public static readonly Success Value = new();
}

[GenerateOneOf]
public partial class SchemaIdentifier : OneOfBase<SchemaName, SchemaVersionId>, IEquatable<SchemaIdentifier> {
	public bool IsSchemaName      => Value is SchemaName;
	public bool IsSchemaVersionId => Value is SchemaVersionId;

	public SchemaName      AsSchemaName      => AsT0;
	public SchemaVersionId AsSchemaVersionId => AsT1;

	public bool Equals(SchemaIdentifier? other) {
		if (other is null) return false;

		if (IsSchemaName != other.IsSchemaName) return false;

		return IsSchemaName
			? AsSchemaName.Value == other.AsSchemaName.Value
			: AsSchemaVersionId.Value == other.AsSchemaVersionId.Value;
	}

	public override bool Equals(object? obj) =>
		obj is SchemaIdentifier other && Equals(other);

	public override int GetHashCode() {
		return IsSchemaName
			? HashCode.Combine(typeof(SchemaName).GetHashCode(), AsSchemaName.Value.GetHashCode())
			: HashCode.Combine(typeof(SchemaVersionId).GetHashCode(), AsSchemaVersionId.Value.GetHashCode());
	}

	public static bool operator ==(SchemaIdentifier? left, SchemaIdentifier? right) =>
		left?.Equals(right) ?? right is null;

	public static bool operator !=(SchemaIdentifier? left, SchemaIdentifier? right) =>
		!(left == right);

}

public class SchemaIdentifierEqualityComparer : IEqualityComparer<SchemaIdentifier> {
	public bool Equals(SchemaIdentifier? x, SchemaIdentifier? y) {
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;

		// Both must be of the same type (either both SchemaName or both SchemaVersionId)
		if (x.IsSchemaName != y.IsSchemaName) return false;

		return x.IsSchemaName
			? x.AsSchemaName.Value == y.AsSchemaName.Value
			: x.AsSchemaVersionId.Value == y.AsSchemaVersionId.Value;
	}

	public int GetHashCode(SchemaIdentifier? obj) {
		if (obj is null) return 0;

		return obj.IsSchemaName
			? obj.AsSchemaName.Value.GetHashCode()
			: obj.AsSchemaVersionId.Value.GetHashCode();
	}
}


[GenerateOneOf]
public partial class CreateSchemaResult : OneOfBase<SchemaVersionDescriptor, ErrorDetails.SchemaAlreadyExists> {
	public bool IsSchemaVersionDescriptor => IsT0;
	public bool IsSchemaAlreadyExists     => IsT1;

	public SchemaVersionDescriptor          AsSchemaVersionDescriptor => AsT0;
	public ErrorDetails.SchemaAlreadyExists AsSchemaAlreadyExists     => AsT1;
}

[GenerateOneOf]
public partial class GetSchemaResult : OneOfBase<Schema, ErrorDetails.SchemaNotFound> {
	public bool IsSchema         => IsT0;
	public bool IsSchemaNotFound => IsT1;

	public Schema                      AsSchema         => AsT0;
	public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class GetSchemaVersionResult : OneOfBase<SchemaVersion, ErrorDetails.SchemaNotFound> {
	public bool IsSchemaVersion  => IsT0;
	public bool IsSchemaNotFound => IsT1;

	public SchemaVersion               AsSchemaVersion  => AsT0;
	public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class DeleteSchemaResult : OneOfBase<Success, ErrorDetails.SchemaNotFound> {
	public bool IsSuccess        => IsT0;
	public bool IsSchemaNotFound => IsT1;

	public Success                     AsSuccess        => AsT0;
	public ErrorDetails.SchemaNotFound AsSchemaNotFound => AsT1;
}

[GenerateOneOf]
public partial class CheckSchemaCompatibilityResult : OneOfBase<SchemaVersionId, SchemaCompatibilityErrors, ErrorDetails.SchemaNotFound> {
	public bool IsSchemaVersionId => IsT0;
	public bool IsSchemaErrors    => IsT1;
	public bool IsSchemaNotFound  => IsT2;

	public SchemaVersionId             AsSchemaVersionId => AsT0;
	public SchemaCompatibilityErrors   AsSchemaErrors    => AsT1;
	public ErrorDetails.SchemaNotFound AsSchemaNotFound  => AsT2;
}
