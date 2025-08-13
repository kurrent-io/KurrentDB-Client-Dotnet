using Grpc.Core;
using Kurrent.Client.Registry;
using NJsonSchema;
using static Kurrent.Client.Streams.SchemaDataFormat;
using static Kurrent.Client.Registry.CompatibilityMode;

namespace Kurrent.Client.Tests.Registry;

public class CheckSchemaCompatibilityTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task backward_mode_incompatible_when_deleting_required_field(CancellationToken ct) {
		// Arrange
		var schemaName  = NewSchemaName();

		var v1 = NewJsonSchemaDefinition().AddRequired("email", JsonObjectType.String);
		var v2 = v1.Remove("email");

		var createResult = await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), Json, Backward, Faker.Lorem.Sentences(), [], ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		await AutomaticClient.Registry
			.CheckSchemaCompatibility(createResult.Value.VersionId, v2.ToJson(), Json, ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(_ => KurrentException.Throw("Expected compatibility check to fail, but it succeeded."))
			.OnFailureAsync(error => {
				error.IsSchemaCompatibilityErrors.ShouldBeTrue();
				var errors = error.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.MissingRequiredProperty);
				errors.ShouldContain(e => e.Details.Contains("Required property in original schema is missing in new schema"));
				errors.ShouldContain(e => e.PropertyPath.Contains("email"));
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_fails_when_schema_not_found(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition();

		// Act & Assert
		await AutomaticClient.Registry
			.CheckSchemaCompatibility(SchemaName.From(schemaName), v1.ToJson(), Json, ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(_ => KurrentException.Throw("Expected compatibility check to fail, but it succeeded."))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_handles_fatal_errors(CancellationToken ct) {
		// Arrange
		var v1 = NewJsonSchemaDefinition();

		// Act & Assert
		var result = async () => await AutomaticClient.Registry
			.CheckSchemaCompatibility(SchemaName.From("#"), v1.ToJson(), Json, ct);

		var exception = await result.ShouldThrowAsync<KurrentException>();
		exception.ErrorCode.ShouldBe(nameof(StatusCode.InvalidArgument));

		exception.Metadata.GetRequired<List<FieldViolation>>("FieldViolations").ShouldSatisfyAllConditions(
			vs => vs.ShouldNotBeEmpty(),
			vs => vs.ShouldHaveSingleItem(),
			vs => vs.ShouldContain(v => v.Field == "SchemaName"),
			vs => vs.ShouldContain(v => v.Description == "Schema name must not be empty and can only contain alphanumeric characters, underscores, dashes, and periods")
		);
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_fails_when_data_format_not_matching(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), Json, Backward, Faker.Lorem.Sentences(), [], ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		await AutomaticClient.Registry
			.CheckSchemaCompatibility(SchemaName.From(schemaName), v1.ToJson(), Protobuf, ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(_ => KurrentException.Throw("Expected compatibility check to fail, but it succeeded."))
			.OnFailureAsync(failure => {
				failure.IsSchemaCompatibilityErrors.ShouldBeTrue();
				var errors = failure.AsSchemaCompatibilityErrors.Errors;
				errors.ShouldContain(e => e.Kind == SchemaCompatibilityErrorKind.DataFormatMismatch);
			});
	}
}
