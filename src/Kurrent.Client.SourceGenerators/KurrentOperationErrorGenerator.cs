using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kurrent.Client.SourceGenerators;

[Generator]
public class KurrentOperationErrorGenerator : ISourceGenerator {
    const string KurrentOperationErrorAttributeKey = "Kurrent.Client.KurrentOperationErrorAttribute";
    const string KurrentClientModelNamespace       = "Kurrent.Client";

    class KurrentOperationErrorSyntaxReceiver : ISyntaxReceiver {
        public List<TypeDeclarationSyntax> CandidateTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode) {
            // Look for partial structs, records, or classes with attributes
            if (syntaxNode is not TypeDeclarationSyntax typeDeclaration ||
                !typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) ||
                typeDeclaration.AttributeLists.Count <= 0) return;

            // Filter for struct or record types
            if (typeDeclaration is StructDeclarationSyntax or RecordDeclarationSyntax) {
                CandidateTypes.Add(typeDeclaration);
            }
        }
    }

    public void Initialize(GeneratorInitializationContext context) =>
        context.RegisterForSyntaxNotifications(() => new KurrentOperationErrorSyntaxReceiver());

    public void Execute(GeneratorExecutionContext context) {
        try {
            var compilation = context.Compilation;

            // Get the syntax receiver
            if (context.SyntaxReceiver is not KurrentOperationErrorSyntaxReceiver receiver)
                return;

            // Debug: Report that we're starting execution
            ReportDiagnostic(context,
                new DiagnosticDescriptor(
                    "Kurrent000",
                    "KurrentOperationErrorSourceGenerator",
                    "Source generator starting execution",
                    "Usage",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true));

            // Process candidate types from the syntax receiver
            foreach (var typeDeclaration in receiver.CandidateTypes) {
                var semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not { } typeSymbol)
                    continue;

                if (!HasKurrentOperationErrorAttribute(typeSymbol, context))
                    continue;

                GenerateAndAddSource(typeSymbol);

                ReportDiagnostic(context,
                    new DiagnosticDescriptor(
                        "Kurrent001",
                        "KurrentOperationErrorSourceGenerator",
                        "Generated KurrentOperationError source for '{0}'",
                        "Usage",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    typeSymbol);
            }
        }
        catch (Exception ex) {
            ReportDiagnostic(context,
                new DiagnosticDescriptor(
                    "Kurrent500",
                    "KurrentOperationErrorSourceGenerator",
                    "Source generator error: {0}",
                    "Error",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                ex.ToString());
        }

        return;

        void GenerateAndAddSource(INamedTypeSymbol typeSymbol) {
            var source   = GenerateImplementation(typeSymbol, context);
            var hintName = GenerateHintName(typeSymbol);
            context.AddSource(hintName, source);
        }
    }

    static void ReportDiagnostic(GeneratorExecutionContext context, DiagnosticDescriptor descriptor, params object?[]? messageArgs) {
        var diagnostic = Diagnostic.Create(descriptor, Location.None, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }

    static void ReportDiagnostic(GeneratorExecutionContext context, DiagnosticDescriptor descriptor, INamedTypeSymbol typeSymbol) {
        var diagnostic = Diagnostic.Create(descriptor, Location.None, typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        context.ReportDiagnostic(diagnostic);
    }

    static IEnumerable<INamedTypeSymbol> ExtractKurrentOperationErrorTypes(Compilation compilation, GeneratorExecutionContext context) {
        foreach (var syntaxTree in compilation.SyntaxTrees) {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var root = syntaxTree.GetRoot();

            var typeDeclarations = root.DescendantNodes()
                .Where(node => node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax);

            // var typeDeclarations = root.DescendantNodes()
            //     .OfType<TypeDeclarationSyntax>()
            //     .Where(td => td.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
            //                  td.Modifiers.Any(m => m.IsKind(SyntaxKind.RecordKeyword) || m.IsKind(SyntaxKind.StructKeyword)));;

            foreach (var typeDeclaration in typeDeclarations) {
                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
                    continue;

                // if has no attributes, skip
                if (typeSymbol.GetAttributes().Length == 0)
                    continue;

                // Skip if the type is not a partial struct or record
                if (typeSymbol is { IsRecord: false, IsValueType: false })
                    continue;

                if (!HasKurrentOperationErrorAttribute(typeSymbol, context))
                    continue;

                // Only generate for partial structs with the attribute
                // We don't check for interface implementation since we'll generate it
                yield return typeSymbol;
            }
        }
    }

    static bool HasKurrentOperationErrorAttribute(INamedTypeSymbol typeSymbol, GeneratorExecutionContext context) {
        var attr = typeSymbol.GetAttributes().Where(Predicate).FirstOrDefault();

        return attr is not null;

        static bool Predicate(AttributeData attr) =>
            attr.AttributeClass!.ToDisplayString().Equals(KurrentOperationErrorAttributeKey);
    }

    static SourceText GenerateImplementation(INamedTypeSymbol typeSymbol, GeneratorExecutionContext context) {
        var sourceBuilder = new StringBuilder(
             """
            // <auto-generated />
            // This file was generated by KurrentOperationErrorGenerator
            // Changes to this file may be lost when the file is regenerated.
            
            #nullable enable
            
            {USING_STATEMENTS}
            
            {NAMESPACE_DECLARATION}
            
            """);

        var (name, ns) = GetInstanceNameAndNamespace(typeSymbol);

        sourceBuilder
            .Replace("{USING_STATEMENTS}", GenerateUsingStatements(ns))
            .Replace("{NAMESPACE_DECLARATION}", GenerateNamespaceDeclaration(ns))
            .AppendLine();

        const string tab = "    ";

        // Generate type declarations for all containing types
        var containingTypeSymbols = new List<INamedTypeSymbol>();
        var currentContainingType = typeSymbol.ContainingType;
        while (currentContainingType is not null) {
            containingTypeSymbols.Add(currentContainingType);
            currentContainingType = currentContainingType.ContainingType;
        }

        containingTypeSymbols.Reverse();

        var declarationIndent = "";
        foreach (var declaration in containingTypeSymbols.Select(GenerateTypeDeclaration)) {
            sourceBuilder.AppendLine($"{declarationIndent}{declaration} {{");
            declarationIndent += tab;
        }

        // Generate the instance type
        var memberIndent = declarationIndent;

        const string codeBlock = """
            
            [PublicAPI, CompilerGenerated, GeneratedCode("KurrentOperationErrorGenerator", "1.0.0")]
            public readonly partial record struct {INSTANCE_NAME} : IResultError {
                static readonly KurrentOperationErrorAttribute Info =
                    KurrentOperationErrorAttribute.GetAttribute(typeof({INSTANCE_NAME}));

                public {INSTANCE_NAME}(Action<Metadata>? configure = null) =>
                    Metadata = new Metadata().Transform(x => configure?.Invoke(x)).Lock();

                public string ErrorCode    => Info.Annotations.Code;
                public string ErrorMessage => Info.Annotations.Message;
                public bool   IsFatal      => Info.Annotations.IsFatal;

                public Metadata Metadata { get; }

                public Exception CreateException(Exception? innerException = null) =>
                    new KurrentException(ErrorCode, ErrorMessage, Metadata, innerException);

                public override string ToString() => ErrorMessage;
            }
            """;

        var indentedCode = Regex.Replace(codeBlock, @"(?<=\n)", memberIndent);

        sourceBuilder
            .Append(memberIndent + indentedCode)
            .Replace("{INSTANCE_NAME}", name);

        // Close all containing types declarations
        sourceBuilder.AppendLine();
        for (var i = containingTypeSymbols.Count - 1; i >= 0; i--) {
            declarationIndent = declarationIndent.Substring(0, declarationIndent.Length - tab.Length);
            sourceBuilder.AppendLine($"{declarationIndent}}}");
        }

        return SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);

        static string GenerateUsingStatements(string ns) {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.CodeDom.Compiler;");
            stringBuilder.AppendLine("using System.Runtime.CompilerServices;");

            if (ns != KurrentClientModelNamespace)
                stringBuilder.AppendLine("using Kurrent.Client.Model;");

            return stringBuilder.ToString().TrimEnd();
        }

        static string GenerateNamespaceDeclaration(string ns) =>
            ns != string.Empty ? $"namespace {ns};" : "// Global namespace";
    }

    static string GenerateTypeDeclaration(INamedTypeSymbol containingTypeSymbol) {
        var typeDeclarationBuilder = new StringBuilder();

        var accessibility = containingTypeSymbol.DeclaredAccessibility switch {
            Accessibility.Public    => "public ",
            Accessibility.Internal  => "internal ",
            Accessibility.Protected => "protected ",
            _                       => string.Empty
        };

        typeDeclarationBuilder.Append(accessibility);

        if (containingTypeSymbol.IsStatic)
            typeDeclarationBuilder.Append("static ");

        if (containingTypeSymbol.IsReadOnly)
            typeDeclarationBuilder.Append("readonly ");
        else if (containingTypeSymbol is { IsAbstract: true, IsSealed: false })
            typeDeclarationBuilder.Append("abstract ");
        else if (containingTypeSymbol is { IsAbstract: false, IsSealed: true })
            typeDeclarationBuilder.Append("sealed ");

        if (IsPartial(containingTypeSymbol))
            typeDeclarationBuilder.Append("partial ");

        var typeKind = containingTypeSymbol switch {
            { IsRecord   : true, IsValueType: true } => "record struct ",
            { IsRecord   : true }                    => "record ",
            { IsValueType: true }                    => "struct ",
            { TypeKind   : TypeKind.Interface }      => "interface ",
            { TypeKind   : TypeKind.Enum }           => "enum ",
            { TypeKind   : TypeKind.Class }          => "class ",
            _                                        => throw new InvalidOperationException($"Unsupported type kind: {containingTypeSymbol.TypeKind}")
        };

        typeDeclarationBuilder.Append(typeKind);
        typeDeclarationBuilder.Append(containingTypeSymbol.Name);

        return typeDeclarationBuilder.ToString();

        static bool IsPartial(INamedTypeSymbol typeSymbol) =>
            typeSymbol.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(syntax => syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
    }

    static (string Name, string ContainingNamespace) GetInstanceNameAndNamespace(INamedTypeSymbol typeSymbol) {
        var instanceName = typeSymbol.Name;
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        return (Name: instanceName, ContainingNamespace: ns);
    }

    static string GenerateHintName(INamedTypeSymbol typeSymbol) {
        var typeNames          = GetInstanceNameAndNamespace(typeSymbol);
        var sanitizedTypeName  = SanitizeName(typeNames.Name);
        var sanitizedNamespace = SanitizeName(typeNames.ContainingNamespace);

        return $"{sanitizedNamespace}_{sanitizedTypeName}.g.cs";

        // Replace spaces, dashes, and pluses with underscores to ensure a valid file name
        // do not replace dots
        string SanitizeName(string input) {
            return input
                .Replace(' ', '_')
                .Replace('-', '_')
                .Replace('+', '_')
                .Replace('<', '_')
                .Replace('>', '_');
        }
    }
}
