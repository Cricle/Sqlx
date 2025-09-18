// -----------------------------------------------------------------------
// <copyright file="DeleteOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core.Operations;

/// <summary>
/// Generator for DELETE operations.
/// </summary>
public class DeleteOperationGenerator : BaseOperationGenerator
{
    /// <inheritdoc/>
    public override string OperationName => "Delete";

    /// <inheritdoc/>
    public override bool CanHandle(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("delete") ||
               methodName.Contains("remove");
    }

    /// <inheritdoc/>
    public override void GenerateOperation(OperationGenerationContext context)
    {
        var sb = context.StringBuilder;
        var method = context.Method;
        var entityType = context.EntityType;
        var tableName = context.TableName;
        var isAsync = context.IsAsync;
        var methodName = context.MethodName;

        // Generate parameter null checks first (fail fast)
        GenerateParameterNullChecks(sb, method);

        // Connection setup
        GenerateConnectionSetup(sb, method, isAsync);

        // 🚀 智能删除 - 支持ID、实体、任意字段删除
        sb.AppendLine("// 🚀 智能删除操作 - 支持ID、实体、任意字段删除");

        // Check parameters in priority order
        var idParam = method.Parameters.FirstOrDefault(p =>
            p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase));
        var entityParam = method.Parameters.FirstOrDefault(p =>
            p.Type.TypeKind == TypeKind.Class &&
            p.Type.Name != "String" &&
            p.Type.Name != "CancellationToken");
        var collectionParam = method.Parameters.FirstOrDefault(p => TypeAnalyzer.IsCollectionType(p.Type));
        var conditionParams = method.Parameters.Where(p =>
            p.Type.SpecialType != SpecialType.None &&
            p.Name != "id" &&
            p.Type.Name != "CancellationToken").ToList();

        var sqlDefine = GetSqlDefineForRepository(method);

        if (collectionParam != null)
        {
            GenerateBatchDelete(sb, collectionParam, tableName, sqlDefine, isAsync, method);
        }
        else if (idParam != null)
        {
            GenerateDeleteById(sb, idParam, tableName, sqlDefine, isAsync, method);
        }
        else if (entityParam != null)
        {
            GenerateDeleteByEntity(sb, entityParam, entityType, tableName, sqlDefine, isAsync, method);
        }
        else if (conditionParams.Any())
        {
            GenerateDeleteByConditions(sb, conditionParams, tableName, sqlDefine, isAsync, method);
        }
        else
        {
            GenerateDeleteAll(sb, tableName, sqlDefine, isAsync, method);
        }

        // Generate method completion
        GenerateMethodCompletion(sb, method, methodName);
    }

    private void GenerateBatchDelete(IndentedStringBuilder sb, IParameterSymbol collectionParam,
        string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Batch delete for {collectionParam.Name}");
        sb.AppendLine($"if ({collectionParam.Name} == null || !{collectionParam.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__result__ = 0;");
        sb.AppendLine("OnExecuted(\"BatchDelete\", __cmd__, __result__, System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__);");
        // Note: Return statement is handled by CodeGenerationService
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Determine if collection contains IDs or entities
        sb.AppendLine("int totalAffected = 0;");
        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();

        // Check if item is a value type (likely ID) or entity
        if (IsValueTypeCollection(collectionParam.Type))
        {
            sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE Id = @id\";");
            sb.AppendLine("__cmd__.Parameters.Clear();");
            sb.AppendLine("var paramId = __cmd__.CreateParameter();");
            sb.AppendLine("paramId.ParameterName = \"@id\";");
            sb.AppendLine("paramId.Value = item;");
            sb.AppendLine("__cmd__.Parameters.Add(paramId);");
        }
        else
        {
            sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE Id = @id\";");
            sb.AppendLine("__cmd__.Parameters.Clear();");
            sb.AppendLine("var paramId = __cmd__.CreateParameter();");
            sb.AppendLine("paramId.ParameterName = \"@id\";");
            sb.AppendLine("paramId.Value = item.Id;");
            sb.AppendLine("__cmd__.Parameters.Add(paramId);");
        }

        GenerateCommandExecution(sb, isAsync, method);
        sb.AppendLine("totalAffected += __result__;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__result__ = totalAffected;");
    }

    private void GenerateDeleteById(IndentedStringBuilder sb, IParameterSymbol idParam,
        string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Delete by ID: {idParam.Name}");
        sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE Id = @id\";");

        sb.AppendLine("var paramId = __cmd__.CreateParameter();");
        sb.AppendLine("paramId.ParameterName = \"@id\";");
        sb.AppendLine($"paramId.Value = {idParam.Name};");
        sb.AppendLine($"paramId.DbType = {GetDbTypeForParameter(idParam)};");
        sb.AppendLine("__cmd__.Parameters.Add(paramId);");
        sb.AppendLine();

        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateDeleteByEntity(IndentedStringBuilder sb, IParameterSymbol entityParam,
        INamedTypeSymbol? entityType, string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Delete by entity: {entityParam.Name}");

        // Try to use ID property first
        var idProperty = entityType?.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name.Equals("Id", System.StringComparison.OrdinalIgnoreCase));

        if (idProperty != null)
        {
            sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE Id = @id\";");
            sb.AppendLine("var paramId = __cmd__.CreateParameter();");
            sb.AppendLine("paramId.ParameterName = \"@id\";");
            sb.AppendLine($"paramId.Value = {entityParam.Name}.Id;");
            sb.AppendLine($"paramId.DbType = {GetDbTypeForPropertyType(idProperty.Type)};");
            sb.AppendLine("__cmd__.Parameters.Add(paramId);");
        }
        else
        {
            // Fallback to all properties as WHERE conditions
            var properties = entityType?.GetMembers().OfType<IPropertySymbol>()
                .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
                .Take(3) // Limit to first 3 properties to avoid overly complex WHERE
                .ToList();

            if (properties?.Any() == true)
            {
                var whereClause = string.Join(" AND ", properties.Select(p => $"{p.Name} = @{p.Name}"));
                sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE {whereClause}\";");

                foreach (var prop in properties)
                {
                    sb.AppendLine($"var param{prop.Name} = __cmd__.CreateParameter();");
                    sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                    sb.AppendLine($"param{prop.Name}.Value = {entityParam.Name}.{prop.Name} ?? (object)global::System.DBNull.Value;");
                    sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForPropertyType(prop.Type)};");
                    sb.AppendLine($"__cmd__.Parameters.Add(param{prop.Name});");
                }
            }
        }

        sb.AppendLine();
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateDeleteByConditions(IndentedStringBuilder sb,
        System.Collections.Generic.List<IParameterSymbol> conditionParams,
        string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine("// Delete by conditions");
        var whereClause = string.Join(" AND ", conditionParams.Select(p => $"{InferColumnNameFromParameter(p.Name)} = @{p.Name}"));
        sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName} WHERE {whereClause}\";");

        foreach (var param in conditionParams)
        {
            sb.AppendLine($"var param{param.Name} = __cmd__.CreateParameter()!;");
            sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
            sb.AppendLine($"param{param.Name}.Value = {param.Name} ?? (object)global::System.DBNull.Value;");
            sb.AppendLine($"param{param.Name}.DbType = {GetDbTypeForParameter(param)};");
            sb.AppendLine($"__cmd__.Parameters.Add(param{param.Name});");
        }

        sb.AppendLine();
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateDeleteAll(IndentedStringBuilder sb, string tableName,
        SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine("// Delete all (use with caution!)");
        sb.AppendLine($"__cmd__.CommandText = \"DELETE FROM {tableName}\";");
        sb.AppendLine();

        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateCommandExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method)
    {
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }
        else
        {
            sb.AppendLine("__result__ = __cmd__.ExecuteNonQuery();");
        }
    }

    private void GenerateMethodCompletion(IndentedStringBuilder sb, IMethodSymbol method, string methodName)
    {
        sb.AppendLine($"OnExecuted(\"{methodName}\", __cmd__, __result__, System.Diagnostics.Stopwatch.GetTimestamp() - __startTime__);");

        if (!method.ReturnsVoid)
        {
            // Note: Return statement is handled by CodeGenerationService
        }
    }


    private bool IsValueTypeCollection(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            var elementType = namedType.TypeArguments[0];
            return elementType.IsValueType || elementType.SpecialType != SpecialType.None;
        }
        return false;
    }

    private string InferColumnNameFromParameter(string parameterName)
    {
        // Convert camelCase to PascalCase for column names
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;

        return char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
    }

    private string GetDbTypeForParameter(IParameterSymbol parameter)
    {
        return GetDbTypeForParameterType(parameter.Type);
    }

    private string GetDbTypeForPropertyType(ITypeSymbol type)
    {
        return GetDbTypeForParameterType(type);
    }

    private string GetDbTypeForParameterType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "global::System.Data.DbType.String",
            SpecialType.System_Int32 => "global::System.Data.DbType.Int32",
            SpecialType.System_Int64 => "global::System.Data.DbType.Int64",
            SpecialType.System_Boolean => "global::System.Data.DbType.Boolean",
            SpecialType.System_DateTime => "global::System.Data.DbType.DateTime",
            SpecialType.System_Decimal => "global::System.Data.DbType.Decimal",
            SpecialType.System_Double => "global::System.Data.DbType.Double",
            _ => "global::System.Data.DbType.Object"
        };
    }
}
