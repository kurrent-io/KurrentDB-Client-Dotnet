using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Kurrent.Variant.SourceGenerators;

[Generator]
public class VariantGenerator : ISourceGenerator {
    public void Initialize(GeneratorInitializationContext context) {
#if DEBUG
        if (!Debugger.IsAttached) Debugger.Launch();
#endif
    }

    public void Execute(GeneratorExecutionContext context) {
        var compilation = context.Compilation;

        foreach (var syntaxTree in compilation.SyntaxTrees) {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var root = syntaxTree.GetRoot();

            // Look for both classes and record structs that implement IVariant<T1, T2, ...>
            var typeDeclarations = root.DescendantNodes()
                .Where(node => node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax);

            foreach (var typeDeclaration in typeDeclarations) {
                if (semanticModel.GetDeclaredSymbol(typeDeclaration) is not INamedTypeSymbol typeSymbol)
                    continue;

                if (!ImplementsIVariantDirectly(typeSymbol, out var genericTypeArguments))
                    continue;

                if (genericTypeArguments.Count != 0) {
                    // Validate IResultError constraints if this implements IVariantResultError
                    if (ImplementsIVariantResultError(typeSymbol)) {
                        if (!ValidateIResultErrorConstraints(genericTypeArguments, context, typeSymbol))
                            continue; // Skip generation if validation fails
                    }

                    var source = GenerateVariantImplementation(typeSymbol, genericTypeArguments, context);
                    if (!string.IsNullOrEmpty(source)) {
                        var sanitizedTypeArguments = string.Join("_", genericTypeArguments.Select(GetSafeFileNameFromType));
                        var fullTypeNameForFile    = $"{typeSymbol.Name}_{sanitizedTypeArguments}";
                        var namespaceForFile       = typeSymbol.ContainingNamespace.IsGlobalNamespace ? "Global" : typeSymbol.ContainingNamespace.ToDisplayString().Replace('.', '_');

                        context.AddSource($"{namespaceForFile}_{fullTypeNameForFile}_Variant.g.cs", SourceText.From(source, Encoding.UTF8));
                    }
                }
            }
        }
    }

    #region . Diagnostic Helpers .

    static readonly DiagnosticDescriptor SwitchMethodsGenerated = new(
        "VARIANT002",
        "Switch methods generated",
        "Generated switch methods for type '{0}'",
        "CodeGeneration",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor MatchMethodsGenerated = new(
        "VARIANT003",
        "Match methods generated",
        "Generated match methods for type '{0}'",
        "CodeGeneration",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static readonly DiagnosticDescriptor ResultErrorImplementationGenerated = new(
        "VARIANT004",
        "Result error implementation generated",
        "Generated result error implementation for type '{0}'",
        "CodeGeneration",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    static void ReportDiagnostic(GeneratorExecutionContext context, DiagnosticDescriptor descriptor, INamedTypeSymbol classSymbol) {
        var diagnostic = Diagnostic.Create(descriptor, Location.None, classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        context.ReportDiagnostic(diagnostic);
    }

    #endregion

    static string GetSafeFileNameFromType(ITypeSymbol typeSymbol) =>
        GetCleanTypeName(typeSymbol)
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "_")
            .Replace("?", "_nullable");

    static bool ImplementsIVariantDirectly(INamedTypeSymbol classSymbol, out List<ITypeSymbol> genericTypeArguments) {
        genericTypeArguments = [];

        foreach (var implementedInterface in classSymbol.Interfaces.Where(Predicate))
            genericTypeArguments.AddRange(implementedInterface.TypeArguments);

        return genericTypeArguments.Count != 0;

        bool Predicate(INamedTypeSymbol implementedInterface) =>
            implementedInterface.ContainingNamespace?.ToDisplayString() == "Kurrent.Variant" &&
            implementedInterface.Name.StartsWith("IVariant") &&
            implementedInterface.IsGenericType;
    }

    static bool ImplementsIVariantResultError(INamedTypeSymbol classSymbol) {
        return classSymbol.Interfaces.Any(Predicate);

        bool Predicate(INamedTypeSymbol implementedInterface) =>
            implementedInterface.ContainingNamespace?.ToDisplayString() == "Kurrent.Variant" &&
            implementedInterface.Name.StartsWith("IVariantResultError") &&
            implementedInterface.IsGenericType;
    }

    static bool ValidateIResultErrorConstraints(List<ITypeSymbol> typeArguments, GeneratorExecutionContext context, INamedTypeSymbol classSymbol) {
        var resultErrorInterface = context.Compilation.GetTypeByMetadataName("Kurrent.IResultError");
        if (resultErrorInterface == null)
            return true; // Can't validate, proceed

        foreach (var typeArg in typeArguments) {
            var implementsIResultError = typeArg.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, resultErrorInterface));

            if (!implementsIResultError) {
                // Generate a diagnostic warning that this type doesn't implement IResultError
                var descriptor = new DiagnosticDescriptor(
                    "VARIANT001",
                    "IVariantResultError type constraint violation",
                    "Type '{0}' in IVariantResultError<...> must implement IResultError",
                    "Variant",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                var diagnostic = Diagnostic.Create(descriptor, Location.None, typeArg.Name);
                context.ReportDiagnostic(diagnostic);
                return false;
            }
        }
        return true;
    }

    static string GenerateVariantImplementation(INamedTypeSymbol classSymbol, List<ITypeSymbol> typeArguments, GeneratorExecutionContext context) {
        var className                   = classSymbol.Name;
        var minimallyQualifiedClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var namespaceName               = classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString();

        var sb = new StringBuilder();
        sb.AppendLine($"// <auto-generated by VariantGenerator for {minimallyQualifiedClassName} @ {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} />");
        sb.AppendLine("#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.");
        sb.AppendLine("// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Kurrent.Variant;");

        sb.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName)) {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        var containingTypeSymbols = new List<INamedTypeSymbol>();
        var currentContainingType = classSymbol.ContainingType;
        while (currentContainingType != null) {
            containingTypeSymbols.Add(currentContainingType);
            currentContainingType = currentContainingType.ContainingType;
        }

        containingTypeSymbols.Reverse();

        foreach (var containingTypeSymbol in containingTypeSymbols) {
            sb.AppendLine($"partial class {containingTypeSymbol.Name} {{");
        }

        var baseIndent = string.Concat(Enumerable.Repeat("    ", containingTypeSymbols.Count));

        // Build the correct interface inheritance
        var typeParams = string.Join(", ", typeArguments.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        sb.AppendLine("[GeneratedCode(\"VariantGenerator\", \"1.0.0\")]");
        sb.AppendLine("[CompilerGenerated]");
        sb.AppendLine($"{baseIndent}public readonly partial record struct {className} {{");
        var memberIndent = $"{baseIndent}    ";

        // Use optimized storage for better performance
        sb.AppendLine($"{memberIndent}readonly object _value;");
        sb.AppendLine($"{memberIndent}readonly byte _index;"); // byte is sufficient for up to 255 variants
        sb.AppendLine();

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg                   = typeArguments[i];
            var typeArgNameForConstructor = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var parameterName             = GetParameterName(typeArg);

            sb.AppendLine($"{memberIndent}public {className}({typeArgNameForConstructor} {parameterName}) {{");

            if (!typeArg.IsValueType && typeArg.SpecialType != SpecialType.System_Nullable_T)
                if (typeArg.IsReferenceType)
                    sb.AppendLine($"{memberIndent}    ArgumentNullException.ThrowIfNull({parameterName});");

            sb.AppendLine($"{memberIndent}    _value = {parameterName};");
            sb.AppendLine($"{memberIndent}    _index = {i};");
            sb.AppendLine();
            sb.AppendLine($"{memberIndent}}}");
        }

        sb.AppendLine();

        // Generate the Case enum
        GenerateCaseEnum(sb, memberIndent, className, typeArguments);

        sb.AppendLine($"{memberIndent}public object Value => _value;");
        sb.AppendLine($"{memberIndent}public int    Index => _index;");
        sb.AppendLine();
        sb.AppendLine($"{memberIndent}public {className}Case Case => ({className}Case)_index;");
        sb.AppendLine();

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg              = typeArguments[i];
            var typeArgNameForMember = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var propertySuffix       = GetCleanTypeName(typeArg);

            sb.AppendLine($"{memberIndent}public bool Is{propertySuffix} => Case == {className}Case.{propertySuffix};");
            sb.AppendLine($"{memberIndent}public {typeArgNameForMember} As{propertySuffix} => Is{propertySuffix} ? ({typeArgNameForMember})Value! : throw new InvalidOperationException($\"Cannot return as {propertySuffix} as current type is {{Value?.GetType().Name ?? \"unknown\"}} (Case {{Case}})\");");
            sb.AppendLine();
        }

        sb.AppendLine("#region . Implicit Operators .");
        sb.AppendLine();
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg                = typeArguments[i];
            var typeArgNameForImplicit = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var parameterName          = GetParameterName(typeArg);
            var propertySuffix         = GetCleanTypeName(typeArg);

            var fullyQualifiedClassName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.AppendLine($"{memberIndent}public static implicit operator {minimallyQualifiedClassName}({typeArgNameForImplicit} _) => new(_);");
            sb.AppendLine($"{memberIndent}public static implicit operator {typeArgNameForImplicit}({minimallyQualifiedClassName} _) => _.As{propertySuffix};");
        }
        sb.AppendLine();
        sb.AppendLine("#endregion");
        sb.AppendLine();

        GenerateSwitchMethods(sb, memberIndent, className, typeArguments);
        ReportDiagnostic(context, SwitchMethodsGenerated, classSymbol);

        GenerateMatchMethods(sb, memberIndent, className, typeArguments);
        ReportDiagnostic(context, MatchMethodsGenerated, classSymbol);

        // GenerateTryPickMethods(sb, memberIndent, className, typeArguments);
        // GenerateGetValueOrDefaultMethods(sb, memberIndent, className, typeArguments);

        if (ImplementsIVariantResultError(classSymbol)) {
            GenerateResultErrorImplementation(sb, memberIndent, className, typeArguments);
            ReportDiagnostic(context, ResultErrorImplementationGenerated, classSymbol);
        }

        if (classSymbol.TypeKind == TypeKind.Struct)
            GenerateToStringOverride(sb, memberIndent);

        sb.AppendLine($"{baseIndent}}}");
        for (var i = containingTypeSymbols.Count - 1; i >= 0; i--)
            sb.AppendLine($"{string.Concat(Enumerable.Repeat("    ", i))}}}");
        return sb.ToString();
    }

    static void GenerateSwitchMethods(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.AppendLine();
        sb.AppendLine($"{indent}#region . Switch .");
        sb.AppendLine();

        sb.Append($"{indent}public void Switch(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            // Ensure paramName is a valid identifier (e.g. if cleanTypeName had invalid chars, though GetCleanTypeName should sanitize)
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Action<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }

        sb.AppendLine(") {");
        sb.AppendLine($"{indent}    switch (Case) {{");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}        case {className}Case.{propertySuffix}: {paramName}(As{propertySuffix}); break;");
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        GenerateSwitchWithState(sb, indent, className, typeArguments);

        GenerateSwitchAsync(sb, indent, className, typeArguments);

        sb.AppendLine($"{indent}#endregion");
    }

    static void GenerateSwitchWithState(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.Append($"{indent}public void Switch<TState>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Action<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, TState> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(", TState state) {");
        sb.AppendLine($"{indent}    switch (Case) {{");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}        case {className}Case.{propertySuffix}: {paramName}(As{propertySuffix}, state); break;");
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    static void GenerateSwitchAsync(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.Append($"{indent}public ValueTask SwitchAsync(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, ValueTask> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(") => Case switch {");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}),");
        }

        sb.AppendLine($"{indent}}};");
        sb.AppendLine();

        sb.Append($"{indent}public ValueTask SwitchAsync<TState>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, TState, ValueTask> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(", TState state) => Case switch {");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}, state),");
        }

        sb.AppendLine($"{indent}}};");
        sb.AppendLine();
    }

    static void GenerateMatchMethods(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.AppendLine();
        sb.AppendLine($"{indent}#region . Match .");
        sb.AppendLine();

        sb.Append($"{indent}public TResult Match<TResult>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName))) paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, TResult> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }

        sb.AppendLine(") => Case switch {");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}),");
        }

        sb.AppendLine($"{indent}}};");
        sb.AppendLine();

        GenerateMatchWithState(sb, indent, className, typeArguments);

        GenerateMatchAsync(sb, indent, className, typeArguments);

        sb.AppendLine($"{indent}#endregion");
    }

    static void GenerateMatchWithState(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.Append($"{indent}public TResult Match<TResult, TState>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, TState, TResult> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(", TState state) => Case switch {");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}, state),");
        }

        // sb.AppendLine($"{indent}    _ => throw new InvalidOperationException(\"Unhandled case in Match statement.\")");
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();
    }

    static void GenerateMatchAsync(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        // All async variant
        sb.Append($"{indent}public ValueTask<TResult> MatchAsync<TResult>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, ValueTask<TResult>> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(") => Case switch {");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}),");
        }

        // sb.AppendLine($"{indent}    _ => throw new InvalidOperationException(\"Unhandled case in MatchAsync statement.\")");
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();

        // Async with state variant
        sb.Append($"{indent}public ValueTask<TResult> MatchAsync<TResult, TState>(");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg       = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            var paramName     = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.Append($"Func<{typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, TState, ValueTask<TResult>> {paramName}");
            if (i < typeArguments.Count - 1) sb.Append(", ");
        }
        sb.AppendLine(", TState state) => Case switch {");

        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg        = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            var cleanTypeName  = GetCleanTypeName(typeArg);
            var paramName      = $"on{cleanTypeName}";
            paramName = SanitizeIdentifier(paramName);
            if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramName)))
                paramName = $"@{paramName}";

            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => {paramName}(As{propertySuffix}, state),");
        }

        // sb.AppendLine($"{indent}    _ => throw new InvalidOperationException(\"Unhandled case in MatchAsync statement.\")");
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();
    }

    static void GenerateCaseEnum(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.AppendLine($"{indent}public enum {className}Case : byte {{");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg = typeArguments[i];
            var cleanTypeName = GetCleanTypeName(typeArg);
            sb.AppendLine($"{indent}    {cleanTypeName} = {i}{(i < typeArguments.Count - 1 ? "," : "")}");
        }
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();
    }

    static string GetCleanTypeName(ITypeSymbol typeSymbol) {
        switch (typeSymbol.SpecialType) {
            case SpecialType.System_Boolean: return "Bool";

            case SpecialType.System_Byte: return "Byte";

            case SpecialType.System_SByte: return "SByte";

            case SpecialType.System_Char: return "Char";

            case SpecialType.System_Decimal: return "Decimal";

            case SpecialType.System_Double: return "Double";

            case SpecialType.System_Single: return "Float";

            case SpecialType.System_Int32: return "Int";

            case SpecialType.System_UInt32: return "UInt";

            case SpecialType.System_Int64: return "Long";

            case SpecialType.System_UInt64: return "ULong";

            case SpecialType.System_Int16: return "Short";

            case SpecialType.System_UInt16: return "UShort";

            case SpecialType.System_Object: return "Object";

            case SpecialType.System_String: return "String";
        }

        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } namedTypeSymbol)
            return $"{GetCleanTypeName(namedTypeSymbol.TypeArguments[0])}Nullable";

        var name = typeSymbol.Name;
        if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType) {
            if (namedType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                name = namedType.Name + string.Join("", namedType.TypeArguments.Select(GetCleanTypeName));
        }
        else if (typeSymbol is IArrayTypeSymbol arrayType) {
            name = $"{GetCleanTypeName(arrayType.ElementType)}Array";
        }

        name = name.Replace(".", "").Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "");
        if (string.IsNullOrEmpty(name) || name == "Nullable`1") {
            if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } nt)
                return $"{GetCleanTypeName(nt.TypeArguments[0])}Nullable";

            return "Type";
        }

        if (name.Length > 0 && char.IsDigit(name[0])) name = $"_{name}";
        return SanitizeIdentifier(name);
    }

    static string SanitizeIdentifier(string name) {
        if (string.IsNullOrEmpty(name)) return "_";

        var idBuilder = new StringBuilder();
        if (name.Length > 0 && !char.IsLetter(name[0]) && name[0] != '_') idBuilder.Append('_');
        foreach (var c in name)
            if (char.IsLetterOrDigit(c) || c == '_')
                idBuilder.Append(c);
            else
                idBuilder.Append('_');

        var result = idBuilder.ToString();
        return string.IsNullOrEmpty(result) ? "GeneratedType" : result;
    }

    static string GetParameterName(ITypeSymbol typeSymbol) {
        string paramNameBase;
        if (typeSymbol is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments.Length: 1 } namedTypeSymbol)
            paramNameBase = $"{GetParameterName(namedTypeSymbol.TypeArguments[0])}Opt";
        else
            switch (typeSymbol.SpecialType) {
                case SpecialType.System_Boolean: paramNameBase = "bool"; break;

                case SpecialType.System_Byte: paramNameBase = "byte"; break;

                case SpecialType.System_SByte: paramNameBase = "sbyte"; break;

                case SpecialType.System_Char: paramNameBase = "char"; break;

                case SpecialType.System_Decimal: paramNameBase = "decimal"; break;

                case SpecialType.System_Double: paramNameBase = "double"; break;

                case SpecialType.System_Single: paramNameBase = "float"; break;

                case SpecialType.System_Int32: paramNameBase = "int"; break;

                case SpecialType.System_UInt32: paramNameBase = "uint"; break;

                case SpecialType.System_Int64: paramNameBase = "long"; break;

                case SpecialType.System_UInt64: paramNameBase = "ulong"; break;

                case SpecialType.System_Int16: paramNameBase = "short"; break;

                case SpecialType.System_UInt16: paramNameBase = "ushort"; break;

                case SpecialType.System_Object: paramNameBase = "object"; break;

                case SpecialType.System_String: paramNameBase = "string"; break;

                default:
                    var cleanTypeName = GetCleanTypeName(typeSymbol);
                    if (string.IsNullOrEmpty(cleanTypeName) || cleanTypeName == "_" || cleanTypeName.EndsWith("Nullable")) {
                        if (cleanTypeName.EndsWith("Nullable")) {
                            // e.g. "IntNullable" -> "intOpt"
                            var baseName = cleanTypeName.Substring(0, cleanTypeName.Length - "Nullable".Length);
                            // Convert baseName (e.g. "Int") to camelCase for the parameter part
                            if (baseName.Length > 0 && char.IsUpper(baseName[0]))
                                baseName = char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
                            else if (string.IsNullOrEmpty(baseName)) // Should not happen if GetCleanTypeName is robust
                                baseName = "value";

                            paramNameBase = $"{baseName}Opt";
                        }
                        else {
                            paramNameBase = "arg";
                        }
                    }
                    else if (char.IsUpper(cleanTypeName[0])) {
                        if (cleanTypeName.Length == 1 || (cleanTypeName.Length > 1 && char.IsLower(cleanTypeName[1]))) {
                            paramNameBase = char.ToLowerInvariant(cleanTypeName[0]) + cleanTypeName.Substring(1);
                        }
                        else {
                            var allUpper = true;
                            foreach (var c_ in cleanTypeName)
                                if (char.IsLower(c_)) {
                                    allUpper = false;
                                    break;
                                }

                            if (allUpper)
                                paramNameBase = cleanTypeName.ToLowerInvariant();
                            else
                                paramNameBase = char.ToLowerInvariant(cleanTypeName[0]) + cleanTypeName.Substring(1);
                        }
                    }
                    else {
                        paramNameBase = cleanTypeName;
                    }

                    break;
            }

        if (SyntaxFacts.IsKeywordKind(SyntaxFacts.GetKeywordKind(paramNameBase)) || paramNameBase == "value")
            return $"@{paramNameBase}";

        return paramNameBase;
    }

	static void GenerateResultErrorImplementation(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.AppendLine();
        sb.AppendLine($"{indent}#region . IResultError .");
        sb.AppendLine();

        // Generate Error property
        sb.AppendLine($"{indent}///<inheritdoc />");
        sb.AppendLine($"{indent}public IResultError Error => (IResultError)_value;");
        sb.AppendLine();

        // Generate ErrorCode property
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Gets the error code from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public string ErrorCode => Error.ErrorCode;");
        sb.AppendLine();

        // Generate ErrorMessage property
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Gets the error message from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public string ErrorMessage => Error.ErrorMessage;");
        sb.AppendLine();

        // Generate CreateException method
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Creates an exception from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"innerException\">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>");
        sb.AppendLine($"{indent}/// <returns>An exception that represents the current error.</returns>");
        sb.AppendLine($"{indent}public Exception CreateException(Exception? innerException = null) => Error.CreateException(innerException);");
        sb.AppendLine();

        // Generate Throw method
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Creates and throws an exception from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"innerException\">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>");
        sb.AppendLine($"{indent}public Exception Throw(Exception? innerException = null) => Error.Throw(innerException);");
        sb.AppendLine();

        sb.AppendLine($"{indent}#endregion");
    }

    static void GenerateToStringOverride(StringBuilder sb, string indent) {
        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Returns a string representation of the current variant value.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public override string? ToString() => _value?.ToString();");
    }

    static void GenerateTryPickMethods(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        for (var i = 0; i < typeArguments.Count; i++) {
            var pickType          = typeArguments[i];
            var pickTypeName      = pickType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var pickTypeCleanName = GetCleanTypeName(pickType);
            sb.AppendLine($"{indent}public bool TryPick{pickTypeCleanName}(out {pickTypeName} value) {{");
            sb.AppendLine($"{indent}    switch (Case) {{");
            sb.AppendLine($"{indent}        case {className}Case.{pickTypeCleanName}:");
            sb.AppendLine($"{indent}            value = As{pickTypeCleanName};");
            sb.AppendLine($"{indent}            return true;");
            sb.AppendLine($"{indent}        default:");
            sb.AppendLine($"{indent}            value = default({pickTypeName})!;");
            sb.AppendLine($"{indent}            return false;");
            sb.AppendLine($"{indent}    }}");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
    }

	static void GenerateGetValueOrDefaultMethods(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        // Generate sync GetValueOrDefault methods for each type
        for (var i = 0; i < typeArguments.Count; i++) {
            var targetType          = typeArguments[i];
            var targetTypeName      = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var targetTypeCleanName = GetCleanTypeName(targetType);

            // Sync version with fallback function
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Gets the {targetTypeCleanName} value if this instance contains a {targetTypeCleanName}; otherwise, returns a fallback value computed from the current value.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <param name=\"fallback\">A function that takes the current value and returns a fallback {targetTypeCleanName} value.</param>");
            sb.AppendLine($"{indent}/// <returns>The {targetTypeCleanName} value if <see cref=\"Is{targetTypeCleanName}\"/> is <c>true</c>; otherwise, the result of the <paramref name=\"fallback\"/> function.</returns>");
            sb.AppendLine($"{indent}public {targetTypeName} Get{targetTypeCleanName}OrDefault(Func<object, {targetTypeName}> fallback) =>");
            sb.AppendLine($"{indent}    TryPick{targetTypeCleanName}(out var value) ? value : fallback(Value);");
            sb.AppendLine();

            // Sync version with fallback function and state
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Gets the {targetTypeCleanName} value if this instance contains a {targetTypeCleanName}; otherwise, returns a fallback value computed from the current value, passing additional state.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <typeparam name=\"TState\">The type of the state to pass to the fallback function.</typeparam>");
            sb.AppendLine($"{indent}/// <param name=\"fallback\">A function that takes the current value and state, and returns a fallback {targetTypeCleanName} value.</param>");
            sb.AppendLine($"{indent}/// <param name=\"state\">The state to pass to the fallback function.</param>");
            sb.AppendLine($"{indent}/// <returns>The {targetTypeCleanName} value if <see cref=\"Is{targetTypeCleanName}\"/> is <c>true</c>; otherwise, the result of the <paramref name=\"fallback\"/> function.</returns>");
            sb.AppendLine($"{indent}public {targetTypeName} Get{targetTypeCleanName}OrDefault<TState>(Func<object, TState, {targetTypeName}> fallback, TState state) =>");
            sb.AppendLine($"{indent}    TryPick{targetTypeCleanName}(out var value) ? value : fallback(Value, state);");
            sb.AppendLine();

            // Async version with fallback function
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Asynchronously gets the {targetTypeCleanName} value if this instance contains a {targetTypeCleanName}; otherwise, returns a fallback value computed from the current value.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <param name=\"fallback\">An asynchronous function that takes the current value and returns a fallback {targetTypeCleanName} value.</param>");
            sb.AppendLine($"{indent}/// <returns>A <see cref=\"ValueTask{{{targetTypeName}}}\"/> representing the asynchronous operation with the {targetTypeCleanName} value or the result of the <paramref name=\"fallback\"/> function.</returns>");
            sb.AppendLine($"{indent}public ValueTask<{targetTypeName}> Get{targetTypeCleanName}OrDefaultAsync(Func<object, ValueTask<{targetTypeName}>> fallback) =>");
            sb.AppendLine($"{indent}    TryPick{targetTypeCleanName}(out var value) ? ValueTask.FromResult(value) : fallback(Value);");
            sb.AppendLine();

            // Async version with fallback function and state
            sb.AppendLine($"{indent}/// <summary>");
            sb.AppendLine($"{indent}/// Asynchronously gets the {targetTypeCleanName} value if this instance contains a {targetTypeCleanName}; otherwise, returns a fallback value computed from the current value, passing additional state.");
            sb.AppendLine($"{indent}/// </summary>");
            sb.AppendLine($"{indent}/// <typeparam name=\"TState\">The type of the state to pass to the fallback function.</typeparam>");
            sb.AppendLine($"{indent}/// <param name=\"fallback\">An asynchronous function that takes the current value and state, and returns a fallback {targetTypeCleanName} value.</param>");
            sb.AppendLine($"{indent}/// <param name=\"state\">The state to pass to the fallback function.</param>");
            sb.AppendLine($"{indent}/// <returns>A <see cref=\"ValueTask{{{targetTypeName}}}\"/> representing the asynchronous operation with the {targetTypeCleanName} value or the result of the <paramref name=\"fallback\"/> function.</returns>");
            sb.AppendLine($"{indent}public ValueTask<{targetTypeName}> Get{targetTypeCleanName}OrDefaultAsync<TState>(Func<object, TState, ValueTask<{targetTypeName}>> fallback, TState state) =>");
            sb.AppendLine($"{indent}    TryPick{targetTypeCleanName}(out var value) ? ValueTask.FromResult(value) : fallback(Value, state);");
            sb.AppendLine();
        }
    }

    static void GenerateCaseBasedResultErrorImplementation(StringBuilder sb, string indent, string className, List<ITypeSymbol> typeArguments) {
        sb.AppendLine($"{indent}#region . IResultError .");
        sb.AppendLine();

        // Generate ErrorCode property
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Gets the error code from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public string ErrorCode => Case switch {{");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => As{propertySuffix}.ErrorCode,");
        }
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();

        // Generate ErrorMessage property
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Gets the error message from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public string ErrorMessage => Case switch {{");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => As{propertySuffix}.ErrorMessage,");
        }
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();


        // /// <summary>
        // /// Creates an exception from the currently stored error.
        // /// </summary>
        // /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        // /// <returns>An exception that represents the current error.</returns>
        // public Exception CreateException(Exception? innerException = null) => ((IResultError)Value).CreateException(innerException);
        //
        // /// <summary>
        // /// Creates and throws an exception from the currently stored error.
        // /// </summary>
        // /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        // public Exception Throw(Exception? innerException = null) => ((IResultError)Value).Throw(innerException);

        // Generate CreateException method
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// Creates an exception from the currently stored error.");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}/// <param name=\"innerException\">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>");
        sb.AppendLine($"{indent}/// <returns>An exception that represents the current error.</returns>");
        sb.AppendLine($"{indent}public Exception CreateException(Exception? innerException = null) => Case switch {{");
        for (var i = 0; i < typeArguments.Count; i++) {
            var typeArg = typeArguments[i];
            var propertySuffix = GetCleanTypeName(typeArg);
            sb.AppendLine($"{indent}    {className}Case.{propertySuffix} => As{propertySuffix}.CreateException(innerException),");
        }
        sb.AppendLine($"{indent}}};");
        sb.AppendLine();

        sb.AppendLine($"{indent}#endregion");
        sb.AppendLine();
    }
}
