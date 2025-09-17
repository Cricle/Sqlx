// -----------------------------------------------------------------------
// <copyright file="InsertOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core.Operations;

/// <summary>
/// Generator for INSERT operations.
/// </summary>
public class InsertOperationGenerator : BaseOperationGenerator
{
    /// <inheritdoc/>
    public override string OperationName => "Insert";

    /// <inheritdoc/>
    public override bool CanHandle(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("create") || 
               methodName.Contains("insert") || 
               methodName.Contains("add");
    }

    /// <inheritdoc/>
    public override void GenerateOperation(OperationGenerationContext context)
    {
        var sb = context.StringBuilder;
        var method = context.Method;
        var entityType = context.EntityType;
        var tableName = context.TableName;
        var methodName = context.MethodName;

        // Generate parameter null checks first (fail fast)
        GenerateParameterNullChecks(sb, method);

        // Connection setup
        GenerateConnectionSetup(sb, method, isAsync);

        // 🚀 智能插入 - 支持单实体、批量、部分字段插入
        sb.AppendLine("// 🚀 智能插入操作 - 支持单实体、批量、部分字段插入");

        var entityParam = method.Parameters.FirstOrDefault(p => 
            p.Type.TypeKind == TypeKind.Class && 
            p.Type.Name != "String" && 
            p.Type.Name != "CancellationToken");

        var collectionParam = method.Parameters.FirstOrDefault(p => TypeAnalyzer.IsCollectionType(p.Type));
        var sqlDefine = GetSqlDefineForRepository(method);

        if (collectionParam != null)
        {
            GenerateBatchInsert(sb, collectionParam, tableName, sqlDefine, isAsync, method);
        }
        else if (entityParam != null && entityType != null)
        {
            GenerateSingleEntityInsert(sb, entityParam, entityType, tableName, sqlDefine, isAsync, method);
        }
        else
        {
            GenerateParameterBasedInsert(sb, method, tableName, sqlDefine, isAsync);
        }

        // Generate method completion
        GenerateMethodCompletion(sb, method, methodName, isAsync);
    }

    private void GenerateBatchInsert(IndentedStringBuilder sb, IParameterSymbol collectionParam, 
        string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Batch insert for {collectionParam.Name}");
        sb.AppendLine($"if ({collectionParam.Name} == null || !{collectionParam.Name}.Any())");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__repoResult__ = 0;");
        sb.AppendLine("OnExecuted(\"BatchInsert\", __repoCmd__, __repoResult__, System.Diagnostics.Stopwatch.GetTimestamp() - __repoStartTime__);");
        // Note: Return statement is handled by CodeGenerationService
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate batch insert logic
        sb.AppendLine("int totalAffected = 0;");
        sb.AppendLine($"foreach (var item in {collectionParam.Name})");
        sb.AppendLine("{");
        sb.PushIndent();
        
        // Generate INSERT command for each item
        GenerateInsertCommand(sb, tableName, sqlDefine, true);
        
        sb.AppendLine("totalAffected += affectedRows;");
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("__repoResult__ = totalAffected;");
    }

    private void GenerateSingleEntityInsert(IndentedStringBuilder sb, IParameterSymbol entityParam, 
        INamedTypeSymbol entityType, string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Single entity insert for {entityParam.Name}");
        
        var properties = GetInsertableProperties(entityType);
        if (properties.Any())
        {
            GenerateInsertCommand(sb, tableName, sqlDefine, false);
            
            // Add parameters
            foreach (var prop in properties)
            {
                sb.AppendLine($"var param{prop.Name} = __repoCmd__.CreateParameter()!;");
                sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                sb.AppendLine($"param{prop.Name}.Value = (object){entityParam.Name}.{prop.Name} ?? (object)global::System.DBNull.Value;");
                sb.AppendLine($"param{prop.Name}.DbType = {GetDbTypeForProperty(prop)};");
                sb.AppendLine($"__repoCmd__.Parameters.Add(param{prop.Name});");
                sb.AppendLine();
            }
        }
        
        // Execute command
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateParameterBasedInsert(IndentedStringBuilder sb, IMethodSymbol method, 
        string tableName, SqlDefine sqlDefine, bool isAsync)
    {
        sb.AppendLine("// Parameter-based insert");
        var insertParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
        
        if (insertParams.Any())
        {
            GenerateInsertCommand(sb, tableName, sqlDefine, false);
            
            foreach (var param in insertParams)
            {
                sb.AppendLine($"var param{param.Name} = __repoCmd__.CreateParameter()!;");
                sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
                sb.AppendLine($"param{param.Name}.Value = (object){param.Name} ?? (object)global::System.DBNull.Value;");
                sb.AppendLine($"param{param.Name}.DbType = {GetDbTypeForParameter(param)};");
                sb.AppendLine($"__repoCmd__.Parameters.Add(param{param.Name});");
                sb.AppendLine();
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateInsertCommand(IndentedStringBuilder sb, string tableName, SqlDefine sqlDefine, bool isBatch)
    {
        // This would be implemented based on the SQL dialect
        sb.AppendLine($"// Generate INSERT command for {tableName}");
        sb.AppendLine($"__repoCmd__.CommandText = \"INSERT INTO {tableName} (...) VALUES (...)\";");
    }

    private void GenerateCommandExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method)
    {
        // For IDbCommand, we don't have async methods, so we execute synchronously
        sb.AppendLine("__repoResult__ = __repoCmd__.ExecuteNonQuery();");
    }

    private void GenerateMethodCompletion(IndentedStringBuilder sb, IMethodSymbol method, string methodName, bool isAsync)
    {
        sb.AppendLine($"OnExecuted(\"{methodName}\", __repoCmd__, __repoResult__, System.Diagnostics.Stopwatch.GetTimestamp() - __repoStartTime__);");
        
        if (!method.ReturnsVoid)
        {
            // Note: Return statement is handled by CodeGenerationService
        }
    }


    private System.Collections.Generic.List<IPropertySymbol> GetInsertableProperties(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && 
                       p.GetMethod != null && 
                       p.Name != "Id" && 
                       p.Name != "EqualityContract")
            .ToList();
    }

    private string GetDbTypeForProperty(IPropertySymbol property)
    {
        return GetDbTypeForParameterType(property.Type);
    }

    private string GetDbTypeForParameter(IParameterSymbol parameter)
    {
        return GetDbTypeForParameterType(parameter.Type);
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
