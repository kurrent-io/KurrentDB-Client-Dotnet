using Kurrent.Client.Registry;
using Kurrent.Client.Streams;

namespace Kurrent.Client.Tests.Registry;

public class GetSchemaTests : KurrentClientTestFixture {
	const int TestTimeoutMs = 20_000;

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema(CancellationToken ct) {
        var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();
        var description      = Faker.Lorem.Sentence();
        var tags             = new Dictionary<string, string> { [Faker.Lorem.Word()] = Faker.Lorem.Word(), [Faker.Lorem.Word()] = Faker.Lorem.Word() };

		var schemaVersion = await AutomaticClient.Registry
            .CreateSchema(
			    schemaName, schemaDefinition, SchemaDataFormat.Json,
			    CompatibilityMode.Backward, description, tags, ct
		    )
            .ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
            .GetSchema(schemaName, cancellationToken: ct)
			.ShouldNotThrowOrFailAsync(schema => {
				schema.LatestSchemaVersion.ShouldBe(schemaVersion.VersionNumber);
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

		var schemaName = NewSchemaName();

		await AutomaticClient.Registry
			.GetSchema(schemaName, cancellationToken: ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(GetSchemaError.GetSchemaErrorCase.NotFound));
	}

	[Test, Timeout(TestTimeoutMs)]
	public async Task get_schema_when_schema_deleted(CancellationToken ct) {
		// Arrange
		var schemaName       = NewSchemaName();
        var schemaDefinition = NewJsonSchemaDefinition().ToJson();

		await AutomaticClient.Registry
            .CreateSchema(schemaName, schemaDefinition, SchemaDataFormat.Json, ct)
            .ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
            .DeleteSchema(schemaName, ct)
            .ShouldNotThrowOrFailAsync();

		await AutomaticClient.Registry
			.GetSchema(schemaName, cancellationToken: ct)
			.ShouldFailAsync(failure =>
                failure.Case.ShouldBe(GetSchemaError.GetSchemaErrorCase.NotFound));
	}
}
