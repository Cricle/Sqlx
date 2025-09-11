// -----------------------------------------------------------------------
// <copyright file="CodeGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Sqlx.Core;

/// <summary>
/// Optimized code generation utilities.
/// </summary>
internal static class CodeGenerator
{
    /// <summary>
    /// Generates method signature with proper formatting.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateMethodSignature(IndentedStringBuilder sb, IMethodSymbol method, bool includeBody = true)
    {
        var isAsync = TypeAnalyzer.IsAsyncType(method.ReturnType);
        var asyncModifier = isAsync ? "async " : "";
        var returnType = method.ReturnType.ToDisplayString();
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));

        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Generated implementation of {method.Name}.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"public {asyncModifier}{returnType} {method.Name}({parameters})");

        if (includeBody)
        {
            sb.AppendLine("{");
            sb.PushIndent();
        }
    }

    /// <summary>
    /// Generates connection setup code.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateConnectionSetup(IndentedStringBuilder sb, bool isAsync)
    {
        sb.AppendLine("if (connection.State != global::System.Data.ConnectionState.Open)");
        sb.AppendLine("{");
        sb.PushIndent();

        if (isAsync)
        {
            sb.AppendLine("await connection.OpenAsync(cancellationToken).ConfigureAwait(false);");
        }
        else
        {
            sb.AppendLine("connection.Open();");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates command creation and SQL assignment.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateCommandSetup(IndentedStringBuilder sb, string sql)
    {
        sb.AppendLine("using var command = connection.CreateCommand();");
        sb.AppendLine("command.CommandType = global::System.Data.CommandType.Text;");
        sb.AppendLine($"command.CommandText = \"{sql.Replace("\"", "\\\"")}\";");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates parameter assignment for entity properties.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateParameterAssignment(IndentedStringBuilder sb, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        foreach (var parameter in method.Parameters)
        {
            if (parameter.Type.Name == "CancellationToken") continue;

            if (TypeAnalyzer.IsLikelyEntityType(parameter.Type))
            {
                // Entity parameter - map properties to parameters
                GenerateEntityParameterMapping(sb, parameter, entityType);
            }
            else
            {
                // Simple parameter
                GenerateSimpleParameter(sb, parameter);
            }
        }

        if (method.Parameters.Length > 0 && !method.Parameters.All(p => p.Type.Name == "CancellationToken"))
        {
            sb.AppendLine();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateEntityParameterMapping(IndentedStringBuilder sb, IParameterSymbol parameter, INamedTypeSymbol? entityType)
    {
        if (entityType == null) return;

        // Use enhanced member analysis that supports primary constructors and records
        var members = PrimaryConstructorAnalyzer.GetAccessibleMembers(entityType)
            .Where(m => m.Name != "Id") // Exclude Id property
            .ToArray();

        foreach (var member in members)
        {
            GenerateParameterCode(sb, parameter.Name, member.Name, member.Name.ToLowerInvariant());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateSimpleParameter(IndentedStringBuilder sb, IParameterSymbol parameter)
    {
        var paramName = parameter.Name.ToLowerInvariant();
        GenerateParameterCode(sb, parameter.Name, parameter.Name, paramName);
    }

    /// <summary>
    /// Generates optimized parameter assignment code.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateParameterCode(IndentedStringBuilder sb, string sourceValue, string propertyName, string paramName)
    {
        var valueExpression = sourceValue == propertyName ? sourceValue : $"{sourceValue}.{propertyName}";
        sb.AppendLine($"var {paramName}Param = command.CreateParameter();");
        sb.AppendLine($"{paramName}Param.ParameterName = \"@{paramName}\";");
        sb.AppendLine($"{paramName}Param.Value = (object?){valueExpression} ?? global::System.DBNull.Value;");
        sb.AppendLine($"command.Parameters.Add({paramName}Param);");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates execution and return logic based on operation type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateExecutionAndReturn(IndentedStringBuilder sb, IMethodSymbol method, SqlOperationType operation, INamedTypeSymbol? entityType, bool isAsync)
    {
        var returnType = method.ReturnType;
        var actualReturnType = TypeAnalyzer.GetInnerType(returnType);
        var cancellationToken = GetCancellationTokenParameter(method);

        switch (operation)
        {
            case SqlOperationType.Select:
                if (TypeAnalyzer.IsCollectionType(actualReturnType))
                {
                    GenerateCollectionReturn(sb, entityType, isAsync, cancellationToken);
                }
                else
                {
                    GenerateSingleEntityReturn(sb, entityType, isAsync, cancellationToken);
                }
                break;

            case SqlOperationType.Insert:
            case SqlOperationType.Update:
            case SqlOperationType.Delete:
                GenerateNonQueryReturn(sb, isAsync, cancellationToken);
                break;

            case SqlOperationType.Scalar:
                GenerateScalarReturn(sb, actualReturnType, isAsync, cancellationToken);
                break;
        }
    }

    private static void GenerateCollectionReturn(IndentedStringBuilder sb, INamedTypeSymbol? entityType, bool isAsync, string cancellationToken)
    {
        var (executeMethod, readMethod, awaitKeyword) = GetAsyncPatterns(isAsync, cancellationToken, "ExecuteReader");

        sb.AppendLine($"using var reader = {awaitKeyword}command.{executeMethod};");
        var entityTypeName = entityType?.ToDisplayString() ?? "object";
        sb.AppendLine($"var results = new global::System.Collections.Generic.List<{entityTypeName}>();");
        sb.AppendLine();
        sb.AppendLine($"while ({awaitKeyword}reader.{readMethod})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType);
            sb.AppendLine("results.Add(entity);");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return results;");
    }

    private static void GenerateSingleEntityReturn(IndentedStringBuilder sb, INamedTypeSymbol? entityType, bool isAsync, string cancellationToken)
    {
        var (executeMethod, readMethod, awaitKeyword) = GetAsyncPatterns(isAsync, cancellationToken, "ExecuteReader");

        sb.AppendLine($"using var reader = {awaitKeyword}command.{executeMethod};");
        sb.AppendLine($"if ({awaitKeyword}reader.{readMethod})");
        sb.AppendLine("{");
        sb.PushIndent();

        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType);
            sb.AppendLine("return entity;");
        }
        else
        {
            sb.AppendLine("return null;");
        }

        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return null;");
    }

    private static void GenerateNonQueryReturn(IndentedStringBuilder sb, bool isAsync, string cancellationToken)
    {
        var (executeMethod, _, awaitKeyword) = GetAsyncPatterns(isAsync, cancellationToken, "ExecuteNonQuery");

        sb.AppendLine($"var result = {awaitKeyword}command.{executeMethod};");
        sb.AppendLine("return result;");
    }

    private static void GenerateScalarReturn(IndentedStringBuilder sb, ITypeSymbol returnType, bool isAsync, string cancellationToken)
    {
        var (executeMethod, _, awaitKeyword) = GetAsyncPatterns(isAsync, cancellationToken, "ExecuteScalar");

        sb.AppendLine($"var result = {awaitKeyword}command.{executeMethod};");
        sb.AppendLine($"return ({returnType.ToDisplayString()})(result ?? default({returnType.ToDisplayString()}))!;");
    }

    /// <summary>
    /// Gets consistent async/sync patterns for database operations.
    /// </summary>
    /// <param name="isAsync">Whether the operation is async</param>
    /// <param name="cancellationToken">Cancellation token parameter</param>
    /// <param name="baseMethod">Base method name (e.g., "ExecuteReader", "ExecuteNonQuery")</param>
    /// <returns>Tuple of (executeMethod, readMethod, awaitKeyword)</returns>
    private static (string executeMethod, string readMethod, string awaitKeyword) GetAsyncPatterns(bool isAsync, string cancellationToken, string baseMethod)
    {
        if (isAsync)
        {
            var executeMethod = baseMethod == "ExecuteReader" ? $"ExecuteReaderAsync()" :
                               baseMethod == "ExecuteNonQuery" ? $"ExecuteNonQueryAsync()" :
                               baseMethod == "ExecuteScalar" ? $"ExecuteScalarAsync()" :
                               $"{baseMethod}Async()";

            var readMethod = $"ReadAsync()";
            return (executeMethod, readMethod, "await ");
        }
        else
        {
            var executeMethod = $"{baseMethod}()";
            var readMethod = "Read()";
            return (executeMethod, readMethod, "");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GenerateEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType)
    {
        // Use the enhanced entity mapping generator that supports primary constructors and records
        EnhancedEntityMappingGenerator.GenerateEntityMapping(sb, entityType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetCancellationTokenParameter(IMethodSymbol method)
    {
        var cancellationTokenParam = method.Parameters.FirstOrDefault(p => p.Type.Name == "CancellationToken");
        return cancellationTokenParam?.Name ?? "default";
    }
}
