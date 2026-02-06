// -----------------------------------------------------------------------
// <copyright file="SqlxVarGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Sqlx;

/// <summary>
/// Source generator for SqlxVar - generates GetVar method and VarProvider property.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class SqlxVarGenerator : IIncrementalGenerator
{
    // Diagnostic descriptors
    private static readonly DiagnosticDescriptor InvalidReturnTypeDescriptor = new(
        id: "SQLX1002",
        title: "Invalid SqlxVar return type",
        messageFormat: "Method '{0}' marked with [SqlxVar] must return string, found '{1}'",
        category: "Sqlx.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidParametersDescriptor = new(
        id: "SQLX1003",
        title: "Invalid SqlxVar method signature",
        messageFormat: "Method '{0}' marked with [SqlxVar] must have zero parameters",
        category: "Sqlx.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateVariableNameDescriptor = new(
        id: "SQLX1001",
        title: "Duplicate SqlxVar variable name",
        messageFormat: "Variable name '{0}' is already defined in class '{1}'. First defined by method '{2}', duplicate in method '{3}'.",
        category: "Sqlx.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find methods with [SqlxVar] attribute and collect diagnostics
        var sqlxVarMethodsWithDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSqlxVarMethod(s),
                transform: static (ctx, _) => GetSqlxVarMethodInfoWithDiagnostics(ctx))
            .Where(static m => m is not null);

        // Report diagnostics
        context.RegisterSourceOutput(
            sqlxVarMethodsWithDiagnostics,
            static (spc, result) =>
            {
                if (result?.Diagnostics != null)
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        spc.ReportDiagnostic(diagnostic);
                    }
                }
            });

        // Extract valid methods only
        var sqlxVarMethods = sqlxVarMethodsWithDiagnostics
            .Select(static (result, _) => result?.MethodInfo)
            .Where(static m => m is not null);

        // Group by containing class
        var methodsByClass = sqlxVarMethods
            .Collect()
            .Select(static (methods, _) => GroupByClass(methods!));

        // Generate code for each class
        context.RegisterSourceOutput(
            methodsByClass,
            static (spc, classes) => Execute(spc, classes));
    }

    private static bool IsSqlxVarMethod(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax method &&
               method.AttributeLists.Count > 0;
    }

    private static SqlxVarMethodResult? GetSqlxVarMethodInfoWithDiagnostics(GeneratorSyntaxContext context)
    {
        var methodDecl = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
        
        if (methodSymbol is null) return null;

        // Check for [SqlxVar] attribute
        var sqlxVarAttr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => 
            {
                var attrName = a.AttributeClass?.Name;
                return attrName == "SqlxVarAttribute" || attrName == "SqlxVar";
            });

        if (sqlxVarAttr is null) return null;

        // Extract variable name from attribute
        if (sqlxVarAttr.ConstructorArguments.Length == 0 ||
            sqlxVarAttr.ConstructorArguments[0].Value is not string variableName)
        {
            return null;
        }

        var diagnostics = new List<Diagnostic>();

        // Validate return type
        if (methodSymbol.ReturnType.SpecialType != SpecialType.System_String)
        {
            var diagnostic = Diagnostic.Create(
                InvalidReturnTypeDescriptor,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name,
                methodSymbol.ReturnType.ToDisplayString());
            diagnostics.Add(diagnostic);
            
            // Return result with diagnostic but no method info
            return new SqlxVarMethodResult(null, diagnostics);
        }

        // Validate parameters
        if (methodSymbol.Parameters.Length != 0)
        {
            var diagnostic = Diagnostic.Create(
                InvalidParametersDescriptor,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name);
            diagnostics.Add(diagnostic);
            
            // Return result with diagnostic but no method info
            return new SqlxVarMethodResult(null, diagnostics);
        }

        var methodInfo = new SqlxVarMethodInfo(
            VariableName: variableName,
            MethodName: methodSymbol.Name,
            IsStatic: methodSymbol.IsStatic,
            ContainingType: methodSymbol.ContainingType,
            Location: methodSymbol.Locations.FirstOrDefault());

        return new SqlxVarMethodResult(methodInfo, diagnostics);
    }

    private static Dictionary<INamedTypeSymbol, List<SqlxVarMethodInfo>> GroupByClass(
        ImmutableArray<SqlxVarMethodInfo?> methods)
    {
        var result = new Dictionary<INamedTypeSymbol, List<SqlxVarMethodInfo>>(
            SymbolEqualityComparer.Default);

        foreach (var method in methods)
        {
            if (method is null) continue;

            if (!result.TryGetValue(method.ContainingType, out var list))
            {
                list = new List<SqlxVarMethodInfo>();
                result[method.ContainingType] = list;
            }

            list.Add(method);
        }

        return result;
    }

    private static void Execute(
        SourceProductionContext context,
        Dictionary<INamedTypeSymbol, List<SqlxVarMethodInfo>> classesByType)
    {
        foreach (var kvp in classesByType)
        {
            var typeSymbol = kvp.Key;
            var methods = kvp.Value;
            
            // Check for duplicate variable names
            var variableNames = new Dictionary<string, SqlxVarMethodInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var method in methods)
            {
                if (variableNames.TryGetValue(method.VariableName, out var existingMethod))
                {
                    // Report duplicate variable name diagnostic
                    var diagnostic = Diagnostic.Create(
                        DuplicateVariableNameDescriptor,
                        method.Location,
                        method.VariableName,
                        typeSymbol.Name,
                        existingMethod.MethodName,
                        method.MethodName);
                    context.ReportDiagnostic(diagnostic);
                    
                    // Skip generating code for this class if there are duplicates
                    continue;
                }
                variableNames[method.VariableName] = method;
            }
            
            // Only generate code if no duplicates were found
            if (variableNames.Count == methods.Count)
            {
                var source = GenerateSource(typeSymbol, methods);
                var fileName = $"{typeSymbol.ToDisplayString().Replace(".", "_")}.SqlxVar.g.cs";
                context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private static string GenerateSource(
        INamedTypeSymbol typeSymbol,
        List<SqlxVarMethodInfo> methods)
    {
        var sb = new StringBuilder();
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : typeSymbol.ContainingNamespace.ToDisplayString();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (ns is not null)
        {
            sb.AppendLine($"namespace {ns};");
            sb.AppendLine();
        }

        sb.AppendLine($"public partial class {typeSymbol.Name}");
        sb.AppendLine("{");

        // Generate GetVar method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets a variable value by name. Auto-generated by SqlxVar source generator.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static string GetVar(object instance, string methodName)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var repo = ({typeSymbol.ToDisplayString()})instance;");
        sb.AppendLine("        return methodName switch");
        sb.AppendLine("        {");

        // Generate switch cases
        foreach (var method in methods)
        {
            var invocation = method.IsStatic
                ? $"{typeSymbol.Name}.{method.MethodName}()"
                : $"repo.{method.MethodName}()";

            sb.AppendLine($"            \"{method.VariableName}\" => {invocation},");
        }

        // Generate default case
        var availableVars = string.Join(", ", methods.Select(m => m.VariableName));
        sb.AppendLine($"            _ => throw new System.ArgumentException(");
        sb.AppendLine($"                $\"Unknown variable: {{methodName}}. Available variables: {availableVars}\",");
        sb.AppendLine($"                nameof(methodName))");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate VarProvider property
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Variable provider function for SqlTemplate integration.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly System.Func<object, string, string> VarProvider = GetVar;");

        sb.AppendLine("}");

        return sb.ToString();
    }
}

/// <summary>
/// Information about a method marked with [SqlxVar].
/// </summary>
internal record SqlxVarMethodInfo(
    string VariableName,
    string MethodName,
    bool IsStatic,
    INamedTypeSymbol ContainingType,
    Location? Location);

/// <summary>
/// Result of analyzing a method with [SqlxVar] attribute, including any diagnostics.
/// </summary>
internal record SqlxVarMethodResult(
    SqlxVarMethodInfo? MethodInfo,
    List<Diagnostic> Diagnostics);
