using Grpc.Core;
using Kurrent.Client.Registry;
using Kurrent.Client.Streams;
using NJsonSchema;
using static Kurrent.Client.Registry.CompatibilityMode;

namespace Kurrent.Client.Tests.Registry;

public class CheckSchemaCompatibilityTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task backward_mode_incompatible_when_deleting_required_field(CancellationToken ct) {
		var schemaName  = NewSchemaName();

		var v1 = NewJsonSchemaDefinition().AddRequired("email", JsonObjectType.String);
		var v2 = v1.Remove("email");

		var schemaVersion = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, Backward, Faker.Lorem.Sentences(), [], ct)
			.ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.CheckSchemaCompatibility(schemaVersion.VersionId, v2.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldFailAsync(failure => {
                failure.Case.ShouldBe(CheckSchemaCompatibilityError.CheckSchemaCompatibilityErrorCase.SchemaCompatibilityErrors);
				var errors = failure.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.MissingRequiredProperty);
				errors.ShouldContain(e => e.Details.Contains("Required property in original schema is missing in new schema"));
				errors.ShouldContain(e => e.PropertyPath.Contains("email"));
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_fails_when_schema_not_found(CancellationToken ct) {
        var schemaName       = SchemaName.From(NewSchemaName());
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

        await AutomaticClient.Registry
			.CheckSchemaCompatibility(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(CheckSchemaCompatibilityError.CheckSchemaCompatibilityErrorCase.NotFound));
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_handles_fatal_errors(CancellationToken ct) {
		var v1 = NewJsonSchemaDefinition();

		var result = async () => await AutomaticClient.Registry
			.CheckSchemaCompatibility(SchemaName.From("#"), v1.ToJson(), SchemaDataFormat.Json, ct);

		var exception = await result.ShouldThrowAsync<KurrentException>();
		exception.ErrorCode.ShouldBe(nameof(StatusCode.InvalidArgument));

		exception.ErrorData.GetRequired<List<FieldViolation>>("FieldViolations").ShouldSatisfyAllConditions(
			vs => vs.ShouldNotBeEmpty(),
			vs => vs.ShouldHaveSingleItem(),
			vs => vs.ShouldContain(v => v.Field == "SchemaName"),
			vs => vs.ShouldContain(v => v.Description == "Schema name must not be empty and can only contain alphanumeric characters, underscores, dashes, and periods")
		);
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_fails_when_data_format_not_matching(CancellationToken ct) {
        var schemaName       = SchemaName.From(NewSchemaName());
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
			.ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.CheckSchemaCompatibility(schemaName, schemaDefinition, SchemaDataFormat.Protobuf, ct)
			.ShouldNotThrowAsync()
			.ShouldFailAsync(failure => {
                failure.Case.ShouldBe(CheckSchemaCompatibilityError.CheckSchemaCompatibilityErrorCase.SchemaCompatibilityErrors);
				var errors = failure.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.DataFormatMismatch);
			});
	}
}
