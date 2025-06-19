using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shouldly;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Kurrent.Client.SourceGenerators;
using TUnit.Core;

namespace Kurrent.Client.SourceGenerators.Tests;

[TestClass]
public class ServiceOperationErrorGeneratorTests {
    
    [Test]
    public async Task generates_correct_implementation_for_attributed_struct() {
        // Arrange
        var source = """
            using Kurrent.Client.Model;
            
            namespace TestNamespace;
            
            [ServiceOperationError(typeof(TestMessage))]
            public readonly partial record struct TestError;
            
            public class TestMessage {
                public static object Descriptor { get; } = new object();
            }
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.ShouldNotBeEmpty();
        var generatedCode = result.GeneratedSources[0].SourceText.ToString();
        
        generatedCode.ShouldContain("public readonly partial record struct TestError(Metadata? Metadata = null) : IResultError");
        generatedCode.ShouldContain("public string ErrorCode =>");
        generatedCode.ShouldContain("public string ErrorMessage =>");
        generatedCode.ShouldContain("public bool IsFatal =>");
        generatedCode.ShouldContain("public Exception CreateException(Exception? innerException = null) =>");
        generatedCode.ShouldContain("public override string ToString() => ErrorMessage;");
    }

    [Test]
    public async Task generates_correct_namespace_for_attributed_struct() {
        // Arrange
        var source = """
            using Kurrent.Client.Model;
            
            namespace MyCustomNamespace.Errors;
            
            [ServiceOperationError(typeof(TestMessage))]
            public readonly partial record struct CustomError;
            
            public class TestMessage {
                public static object Descriptor { get; } = new object();
            }
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.ShouldNotBeEmpty();
        var generatedCode = result.GeneratedSources[0].SourceText.ToString();
        
        generatedCode.ShouldContain("namespace MyCustomNamespace.Errors;");
    }

    [Test]
    public async Task generates_error_code_from_struct_name() {
        // Arrange
        var source = """
            using Kurrent.Client.Model;
            
            [ServiceOperationError(typeof(TestMessage))]
            public readonly partial record struct StreamNotFound;
            
            public class TestMessage {
                public static object Descriptor { get; } = new object();
            }
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.ShouldNotBeEmpty();
        var generatedCode = result.GeneratedSources[0].SourceText.ToString();
        
        // Should contain fallback error code generation logic
        generatedCode.ShouldContain("STREAM_NOT_FOUND");
    }

    [Test]
    public async Task ignores_structs_without_attribute() {
        // Arrange
        var source = """
            namespace TestNamespace;
            
            public readonly partial record struct RegularStruct;
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.ShouldBeEmpty();
    }

    [Test]
    public async Task handles_multiple_attributed_structs() {
        // Arrange
        var source = """
            using Kurrent.Client.Model;
            
            namespace TestNamespace;
            
            [ServiceOperationError(typeof(TestMessage1))]
            public readonly partial record struct Error1;
            
            [ServiceOperationError(typeof(TestMessage2))]
            public readonly partial record struct Error2;
            
            public class TestMessage1 {
                public static object Descriptor { get; } = new object();
            }
            
            public class TestMessage2 {
                public static object Descriptor { get; } = new object();
            }
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.Length.ShouldBe(2);
        
        var source1 = result.GeneratedSources[0].SourceText.ToString();
        var source2 = result.GeneratedSources[1].SourceText.ToString();
        
        (source1.Contains("Error1") || source2.Contains("Error1")).ShouldBeTrue();
        (source1.Contains("Error2") || source2.Contains("Error2")).ShouldBeTrue();
    }

    [Test]
    public async Task generates_usings_and_nullable_directive() {
        // Arrange
        var source = """
            using Kurrent.Client.Model;
            
            [ServiceOperationError(typeof(TestMessage))]
            public readonly partial record struct TestError;
            
            public class TestMessage {
                public static object Descriptor { get; } = new object();
            }
            """;

        // Act
        var result = await GenerateSource(source);

        // Assert
        result.GeneratedSources.ShouldNotBeEmpty();
        var generatedCode = result.GeneratedSources[0].SourceText.ToString();
        
        generatedCode.ShouldContain("// <auto-generated/>");
        generatedCode.ShouldContain("#nullable enable");
        generatedCode.ShouldContain("using System;");
        generatedCode.ShouldContain("using Kurrent;");
        generatedCode.ShouldContain("using Kurrent.Client.Model;");
    }

    private static async Task<GeneratorDriverRunResult> GenerateSource(string source) {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Reference the necessary assemblies
        var references = new List<MetadataReference>();
        
        // Add basic .NET references
        var assemblies = new[] {
            typeof(object).Assembly,                    // System.Private.CoreLib
            typeof(Console).Assembly,                   // System.Console
            typeof(IEnumerable<>).Assembly,            // System.Collections
            Assembly.Load("System.Runtime"),           // System.Runtime
            Assembly.Load("Microsoft.CSharp")          // Microsoft.CSharp
        };

        foreach (var assembly in assemblies) {
            references.Add(MetadataReference.CreateFromFile(assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new ServiceOperationErrorGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }
}