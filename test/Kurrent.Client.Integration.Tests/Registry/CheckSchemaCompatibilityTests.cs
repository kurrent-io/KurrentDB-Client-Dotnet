using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using NJsonSchema;
using static Kurrent.Client.Model.SchemaDataFormat;
using static Kurrent.Client.SchemaRegistry.CompatibilityMode;
using ErrorDetails = Kurrent.Client.SchemaRegistry.ErrorDetails;

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
			.OnSuccessAsync(_ => KurrentClientException.Throw("Expected compatibility check to fail, but it succeeded."))
			.OnErrorAsync(error => {
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
			.OnSuccessAsync(_ => KurrentClientException.Throw("Expected compatibility check to fail, but it succeeded."))
			.OnErrorAsync(error => {
				error.IsSchemaNotFound.ShouldBeTrue();
				error.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				error.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task check_schema_compatibility_fails_when_data_format_not_matching(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		var v1 = NewJsonSchemaDefinition();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), Json, Backward, Faker.Lorem.Sentences(), [], ct)
			.ShouldNotThrowAsync();

		// Act
		var result = async () => await AutomaticClient.Registry
			.CheckSchemaCompatibility(SchemaName.From(schemaName), v1.ToJson(), Protobuf, ct);

		// Assert
		var exception = await result.ShouldThrowAsync<KurrentClientException>();

		exception.FieldViolations.ShouldNotBeEmpty();
		exception.FieldViolations.ShouldNotBeEmpty();
		exception.FieldViolations.ShouldContain(v => v.Field == "DataFormat");
		exception.FieldViolations.ShouldContain(v => v.Description == "Schema format must be JSON for compatibility check");
	}
}
