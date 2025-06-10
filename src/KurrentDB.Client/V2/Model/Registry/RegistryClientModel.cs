using System.Runtime.InteropServices;
using OneOf;

namespace Kurrent.Client.SchemaRegistry;

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
