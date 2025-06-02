using KurrentDB.Client.Model;
using KurrentDB.Client.SchemaRegistry;

namespace Kurrent.Client.Tests;

public class JsonSchemaNetSchemaExporterTests {
	[Test]
	public void works() {
		var exporter2 = new SchemaExporter();

		var schema2 = exporter2.Export(typeof(SchemaVersion), SchemaDataFormat.Json);

	}
}
