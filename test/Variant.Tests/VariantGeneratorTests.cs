
using Kurrent.Variant.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace Kurrent.Variant.Tests;

public class VariantGeneratorTests {
    // [Test]
    public void should_generate_expected_code() {
        // Create a test compilation
        var source = """
            using Kurrent.Variant;
            public readonly partial record struct ReplicatorVariant : IVariant<string, int> { }
            """;
        var compilation = CreateCompilation(source);
        var generator   = new VariantGenerator();

        // Run the generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        // Get the generated source
        var result          = driver.GetRunResult();
        var generatedSource = result.GeneratedTrees.First().ToString();

        // Assert expected patterns exist
        generatedSource.ShouldContain("public bool IsString");
        generatedSource.ShouldContain("public string AsString");
        generatedSource.ShouldContain("public bool IsInt");
    }

    static Compilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "compilation",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(System.Reflection.Binder).GetTypeInfo().Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );
}
