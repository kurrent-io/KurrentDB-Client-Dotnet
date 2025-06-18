using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using ErrorDetails = Kurrent.Client.SchemaRegistry.ErrorDetails;

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
			.OnErrorAsync(error => Should.RecordException(error.ToException()))
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
			.OnErrorAsync(error => {
				var exception = error.ToException().ShouldBeOfType<KurrentClientException>();
				exception.Message.ShouldBe($"Schema '{schemaName}' not found.");
				exception.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
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
			.OnErrorAsync(error => {
				var exception = error.ToException().ShouldBeOfType<KurrentClientException>();
				exception.Message.ShouldBe($"Schema '{schemaName}' not found.");
				exception.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
			});
	}
}
