using Kurrent.Client.Model;
using Kurrent.Client.SchemaRegistry;
using ErrorDetails = Kurrent.Client.SchemaRegistry.ErrorDetails;

namespace Kurrent.Client.Tests.Registry;

public class DeleteSchemaTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task deletes_schema_throws_not_found_when_schema_does_not_exist(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();

		// Act & Assert
		await AutomaticClient.Registry
			.DeleteSchema(schemaName, ct)
			.ShouldNotThrowAsync()
			.OnErrorAsync(error => {
				error.IsSchemaNotFound.ShouldBeTrue();
				error.AsSchemaNotFound.ErrorCode.ShouldBe(nameof(ErrorDetails.SchemaNotFound));
				error.AsSchemaNotFound.ErrorMessage.ShouldBe($"Schema '{schemaName}' not found.");
			});
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task deletes_existing_schema(CancellationToken ct) {
		// Arrange
		var schemaName = NewSchemaName();
		var v1 = NewJsonSchemaDefinition();

		await AutomaticClient.Registry
			.CreateSchema(schemaName, v1.ToJson(), SchemaDataFormat.Json, ct)
			.ShouldNotThrowAsync();

		// Act & Assert
		await AutomaticClient.Registry
			.DeleteSchema(schemaName, ct)
			.ShouldNotThrowAsync()
			.OnSuccessAsync(success => success.ShouldBe(Success.Instance));
	}
}
