using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Registry;

public class GetSchemaTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema(CancellationToken ct) {
		// Arrange
		var schemaName  = NewSchemaName();
		var description = Faker.Lorem.Sentence();
		var tags        = new Dictionary<string, string> { [Faker.Lorem.Word()] = Faker.Lorem.Word(), [Faker.Lorem.Word()] = Faker.Lorem.Word() };

		var v1 = NewJsonSchemaDefinition();

		var createdSchemaResult = await AutomaticClient.Registry.CreateSchema(
			schemaName, v1.ToJson(), SchemaDataFormat.Json,
			CompatibilityMode.Backward, description, tags,
			ct
		);

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchema(schemaName, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnFailureAsync(failure => KurrentClientException.Throw(failure))
			.OnSuccessAsync(schema => {
				schema.LatestSchemaVersion.ShouldBe(createdSchemaResult.Value.VersionNumber);
				schema.SchemaName.Value.ShouldBe(schemaName);
				schema.CreatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
				schema.UpdatedAt.ShouldBeGreaterThan(DateTimeOffset.MinValue);
				schema.Details.Compatibility.ShouldBe(CompatibilityMode.Backward);
				schema.Details.Description.ShouldBe(description);
				schema.Details.Tags.ShouldBe(tags);
				schema.Details.DataFormat.ShouldBe(SchemaDataFormat.Json);
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_when_schema_does_no_exists(CancellationToken ct) {
		// Arrange
		var schemaName  = NewSchemaName();

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchema(schemaName, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(_ => KurrentClientException.Throw("Expected schema not found, but got a schema response."))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_when_schema_deleted(CancellationToken ct) {
		// Arrange
		var schemaName  = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		await AutomaticClient.Registry.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct);
		await AutomaticClient.Registry.DeleteSchema(schemaName, ct);

		// Act & Assert
		await AutomaticClient.Registry
			.GetSchema(schemaName, cancellationToken: ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(schema => KurrentClientException.Throw("Expected schema not found, but got a schema response."))
			.OnFailureAsync(failure => {
				failure.IsSchemaNotFound.ShouldBeTrue();
				failure.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				failure.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}
}
