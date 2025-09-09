// -----------------------------------------------------------------------
// <copyright file="MethodTemplate.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// Simplified method generation templates.
/// </summary>
internal static class MethodTemplate
{
    /// <summary>
    /// Generates a simplified method implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateMethod(IndentedStringBuilder sb, IMethodSymbol method, string sql, bool isAsync)
    {
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        var returnType = method.ReturnType.ToDisplayString();
        var asyncModifier = isAsync ? "async " : "";
        var awaitKeyword = isAsync ? "await " : "";
        var cancellationToken = isAsync ? GetCancellationTokenParam(method) : "";

        sb.AppendLine($"public {asyncModifier}{returnType} {methodName}({parameters})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Simple try-catch structure
        sb.AppendLine("try");
        sb.AppendLine("{");
        sb.PushIndent();

        // Connection management
        GenerateConnectionSetup(sb, isAsync);

        // Command setup
        sb.AppendLine($"using var cmd = connection.CreateCommand();");
        sb.AppendLine($"cmd.CommandText = {sql};");

        // Parameter binding (simplified)
        GenerateParameterBinding(sb, method);

        // Execute and return
        GenerateExecution(sb, method, isAsync, awaitKeyword, cancellationToken);

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("catch (Exception ex)");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("throw new InvalidOperationException($\"Error executing {methodName}: {ex.Message}\", ex);");
        sb.PopIndent();
        sb.AppendLine("}");

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateConnectionSetup(IndentedStringBuilder sb, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("if (connection.State != ConnectionState.Open)");
            sb.AppendLine("    await connection.OpenAsync();");
        }
        else
        {
            sb.AppendLine("if (connection.State != ConnectionState.Open)");
            sb.AppendLine("    connection.Open();");
        }
        sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateParameterBinding(IndentedStringBuilder sb, IMethodSymbol method)
    {
        var parameters = method.Parameters.Where(p => 
            p.Type.Name != "CancellationToken" && 
            !p.GetAttributes().Any(a => a.AttributeClass?.Name == "ExpressionToSqlAttribute")).ToArray();

        foreach (var param in parameters)
        {
            GenerateParameterCode(sb, param);
        }
        
        if (parameters.Any())
            sb.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateParameterCode(IndentedStringBuilder sb, IParameterSymbol param)
    {
        sb.AppendLine($"var param{param.Name} = cmd.CreateParameter();");
        sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
        sb.AppendLine($"param{param.Name}.Value = {param.Name} ?? DBNull.Value;");
        sb.AppendLine($"cmd.Parameters.Add(param{param.Name});");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateExecution(IndentedStringBuilder sb, IMethodSymbol method, bool isAsync, string awaitKeyword, string cancellationToken)
    {
        var returnType = method.ReturnType;
        
        if (method.ReturnsVoid || (isAsync && returnType.Name == "Task"))
        {
            // Non-query operation
            sb.AppendLine($"{awaitKeyword}cmd.ExecuteNonQuery{(isAsync ? "Async" : "")}({cancellationToken});");
        }
        else if (IsScalarType(returnType))
        {
            // Scalar operation
            sb.AppendLine($"var result = {awaitKeyword}cmd.ExecuteScalar{(isAsync ? "Async" : "")}({cancellationToken});");
            sb.AppendLine($"return ({returnType.ToDisplayString()})(result ?? default({returnType.ToDisplayString()}));");
        }
        else if (IsCollectionType(returnType))
        {
            // Collection operation
            GenerateCollectionReturn(sb, returnType, isAsync, cancellationToken);
        }
        else
        {
            // Single entity operation
            GenerateSingleReturn(sb, returnType, isAsync, cancellationToken);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateCollectionReturn(IndentedStringBuilder sb, ITypeSymbol returnType, bool isAsync, string cancellationToken)
    {
        var elementType = GetElementType(returnType);
        var awaitKeyword = isAsync ? "await " : "";
        var asyncSuffix = isAsync ? "Async" : "";
        
        sb.AppendLine($"using var reader = {awaitKeyword}cmd.ExecuteReader{asyncSuffix}({cancellationToken});");
        sb.AppendLine($"var results = new List<{elementType.ToDisplayString()}>();");
        sb.AppendLine();
        sb.AppendLine($"while ({awaitKeyword}reader.Read{asyncSuffix}({cancellationToken}))");
        sb.AppendLine("{");
        sb.PushIndent();
        GenerateEntityMapping(sb, elementType);
        sb.AppendLine("results.Add(entity);");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return results;");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateSingleReturn(IndentedStringBuilder sb, ITypeSymbol returnType, bool isAsync, string cancellationToken)
    {
        var awaitKeyword = isAsync ? "await " : "";
        var asyncSuffix = isAsync ? "Async" : "";
        
        sb.AppendLine($"using var reader = {awaitKeyword}cmd.ExecuteReader{asyncSuffix}({cancellationToken});");
        sb.AppendLine($"if ({awaitKeyword}reader.Read{asyncSuffix}({cancellationToken}))");
        sb.AppendLine("{");
        sb.PushIndent();
        GenerateEntityMapping(sb, returnType);
        sb.AppendLine("return entity;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("return null;");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateEntityMapping(IndentedStringBuilder sb, ITypeSymbol entityType)
    {
        sb.AppendLine($"var entity = new {entityType.ToDisplayString()}();");
        
        if (entityType is INamedTypeSymbol namedType)
        {
            var properties = namedType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public).ToArray();

            foreach (var prop in properties)
            {
                var ordinal = $"reader.GetOrdinal(\"{prop.Name.ToLowerInvariant()}\")";
                sb.AppendLine($"if (!reader.IsDBNull({ordinal}))");
                sb.AppendLine($"    entity.{prop.Name} = reader.Get{GetReaderMethod(prop.Type)}({ordinal});");
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetReaderMethod(ITypeSymbol type)
    {
        return type.Name switch
        {
            "String" => "String",
            "Int32" => "Int32",
            "Int64" => "Int64",
            "Boolean" => "Boolean",
            "DateTime" => "DateTime",
            "Decimal" => "Decimal",
            "Double" => "Double",
            "Float" => "Float",
            "Guid" => "Guid",
            _ => "Value"
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetCancellationTokenParam(IMethodSymbol method)
    {
        var tokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        return tokenParam != null ? tokenParam.Name : "CancellationToken.None";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalarType(ITypeSymbol type)
    {
        if (type.Name == "Task" && type is INamedTypeSymbol taskType && taskType.TypeArguments.Length == 1)
            type = taskType.TypeArguments[0];

        return type.Name is "Int32" or "String" or "Boolean" or "DateTime" or "Decimal" or "Int64" or "Double" or "Guid";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type.Name == "Task" && type is INamedTypeSymbol taskType && taskType.TypeArguments.Length == 1)
            type = taskType.TypeArguments[0];

        return type.Name.StartsWith("IList") || type.Name.StartsWith("List") || 
               type.Name.StartsWith("IEnumerable") || type.Name.StartsWith("ICollection");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ITypeSymbol GetElementType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];
        
        return type;
    }
}
