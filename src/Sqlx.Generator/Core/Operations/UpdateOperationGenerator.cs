// -----------------------------------------------------------------------
// <copyright file="UpdateOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core.Operations;

/// <summary>
/// Generator for UPDATE operations.
/// </summary>
public class UpdateOperationGenerator : BaseOperationGenerator
{
    /// <inheritdoc/>
    public override string OperationName => "Update";

    /// <inheritdoc/>
    public override bool CanHandle(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("update") || 
               methodName.Contains("modify") ||
               methodName.Contains("change");
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

        // 🚀 智能更新 - 支持实体更新、部分更新、条件更新
        sb.AppendLine("// 🚀 智能更新操作 - 支持实体更新、部分更新、条件更新");

        var entityParam = method.Parameters.FirstOrDefault(p => 
            p.Type.TypeKind == TypeKind.Class && 
            p.Type.Name != "String" && 
            p.Type.Name != "CancellationToken");

        var collectionParam = method.Parameters.FirstOrDefault(p => TypeAnalyzer.IsCollectionType(p.Type));
        var sqlDefine = GetSqlDefineForRepository(method);

        if (IsSmartUpdateMethod(method))
        {
            GenerateSmartUpdate(sb, method, entityType, tableName, isAsync);
        }
        else if (collectionParam != null)
        {
            GenerateBatchUpdate(sb, collectionParam, tableName, sqlDefine, isAsync, method);
        }
        else if (entityParam != null && entityType != null)
        {
            GenerateSingleEntityUpdate(sb, entityParam, entityType, tableName, sqlDefine, isAsync, method);
        }
        else
        {
            GenerateParameterBasedUpdate(sb, method, tableName, sqlDefine, isAsync);
        }

        // Generate method completion
        GenerateMethodCompletion(sb, method, methodName);
    }

    private bool IsSmartUpdateMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("partial") || 
               methodName.Contains("increment") || 
               methodName.Contains("optimistic") ||
               methodName.Contains("bulk");
    }

    private void GenerateSmartUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        var methodName = method.Name.ToLowerInvariant();
        
        if (methodName.Contains("partial"))
        {
            GeneratePartialUpdate(sb, method, entityType, tableName, isAsync);
        }
        else if (methodName.Contains("increment"))
        {
            GenerateIncrementUpdate(sb, method, tableName, isAsync);
        }
        else if (methodName.Contains("optimistic"))
        {
            GenerateOptimisticUpdate(sb, method, entityType, tableName, isAsync);
        }
        else if (methodName.Contains("bulk"))
        {
            GenerateBulkUpdate(sb, method, entityType, tableName, isAsync);
        }
    }

    private void GeneratePartialUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        sb.AppendLine("// Partial update - only specified fields");
        var updateParams = method.Parameters.Where(p => 
            p.Type.Name != "CancellationToken" && 
            !p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase)).ToList();
        
        var idParam = method.Parameters.FirstOrDefault(p => 
            p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase));

        if (updateParams.Any() && idParam != null)
        {
            var setClause = string.Join(", ", updateParams.Select(p => $"{p.Name} = @{p.Name}"));
            sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE Id = @id\";");
            
            // Add parameters
            foreach (var param in updateParams.Concat(new[] { idParam }))
            {
                GenerateParameterCode(sb, param);
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateIncrementUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        string tableName, bool isAsync)
    {
        sb.AppendLine("// Increment update - atomic increment/decrement");
        var incrementParams = method.Parameters.Where(p => 
            p.Type.Name != "CancellationToken" && 
            IsNumericType(p.Type) &&
            !p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase)).ToList();
        
        var idParam = method.Parameters.FirstOrDefault(p => 
            p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase));

        if (incrementParams.Any() && idParam != null)
        {
            var setClause = string.Join(", ", incrementParams.Select(p => $"{p.Name} = {p.Name} + @{p.Name}"));
            sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE Id = @id\";");
            
            foreach (var param in incrementParams.Concat(new[] { idParam }))
            {
                GenerateParameterCode(sb, param);
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateOptimisticUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        sb.AppendLine("// Optimistic update with version checking");
        var entityParam = method.Parameters.FirstOrDefault(p => 
            p.Type.TypeKind == TypeKind.Class && 
            p.Type.Name != "String" && 
            p.Type.Name != "CancellationToken");

        if (entityParam != null && entityType != null)
        {
            var updatableProps = GetUpdatableProperties(entityType);
            var versionProp = entityType.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name.ToLowerInvariant().Contains("version") || 
                                   p.Name.ToLowerInvariant().Contains("timestamp"));

            if (updatableProps.Any())
            {
                var setClause = string.Join(", ", updatableProps.Select(p => $"{p.Name} = @{p.Name}"));
                var whereClause = versionProp != null ? 
                    $"Id = @Id AND {versionProp.Name} = @Original{versionProp.Name}" : 
                    "Id = @Id";
                
                sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE {whereClause}\";");
                
                foreach (var prop in updatableProps)
                {
                    sb.AppendLine($"var param{prop.Name} = __repoCmd__.CreateParameter()!;");
                    sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                    sb.AppendLine($"param{prop.Name}.Value = (object){entityParam.Name}.{prop.Name} ?? (object)global::System.DBNull.Value;");
                    sb.AppendLine($"__repoCmd__.Parameters.Add(param{prop.Name});");
                }
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateBulkUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        sb.AppendLine("// Bulk update for multiple entities");
        var collectionParam = method.Parameters.FirstOrDefault(p => TypeAnalyzer.IsCollectionType(p.Type));
        
        if (collectionParam != null)
        {
            sb.AppendLine($"if ({collectionParam.Name} == null || !{collectionParam.Name}.Any())");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine("__repoResult__ = 0;");
            // Note: Return statement is handled by CodeGenerationService
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("int totalAffected = 0;");
            sb.AppendLine($"foreach (var item in {collectionParam.Name})");
            sb.AppendLine("{");
            sb.PushIndent();
            
            // Generate update for each item
            if (entityType != null)
            {
                var updatableProps = GetUpdatableProperties(entityType);
                if (updatableProps.Any())
                {
                    var setClause = string.Join(", ", updatableProps.Select(p => $"{p.Name} = @{p.Name}"));
                    sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE Id = @Id\";");
                    sb.AppendLine("__repoCmd__.Parameters.Clear();");
                    
                    foreach (var prop in updatableProps)
                    {
                        sb.AppendLine($"var param{prop.Name} = __repoCmd__.CreateParameter()!;");
                        sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                        sb.AppendLine($"param{prop.Name}.Value = item.{prop.Name} ?? (object)global::System.DBNull.Value;");
                        sb.AppendLine($"__repoCmd__.Parameters.Add(param{prop.Name});");
                    }
                }
            }
            
            GenerateCommandExecution(sb, isAsync, method, "item");
            sb.AppendLine("totalAffected += __repoResult__;");
            sb.PopIndent();
            sb.AppendLine("}");
            sb.AppendLine("__repoResult__ = totalAffected;");
        }
    }

    private void GenerateBatchUpdate(IndentedStringBuilder sb, IParameterSymbol collectionParam, 
        string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Batch update for {collectionParam.Name}");
        GenerateBulkUpdate(sb, method, null, tableName, isAsync);
    }

    private void GenerateSingleEntityUpdate(IndentedStringBuilder sb, IParameterSymbol entityParam, 
        INamedTypeSymbol entityType, string tableName, SqlDefine sqlDefine, bool isAsync, IMethodSymbol method)
    {
        sb.AppendLine($"// Single entity update for {entityParam.Name}");
        
        var updatableProps = GetUpdatableProperties(entityType);
        if (updatableProps.Any())
        {
            var setClause = string.Join(", ", updatableProps.Select(p => $"{p.Name} = @{p.Name}"));
            sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE Id = @Id\";");
            
            // Add parameters for updatable properties
            foreach (var prop in updatableProps)
            {
                sb.AppendLine($"var param{prop.Name} = __repoCmd__.CreateParameter()!;");
                sb.AppendLine($"param{prop.Name}.ParameterName = \"@{prop.Name}\";");
                sb.AppendLine($"param{prop.Name}.Value = (object){entityParam.Name}.{prop.Name} ?? (object)global::System.DBNull.Value;");
                sb.AppendLine($"__repoCmd__.Parameters.Add(param{prop.Name});");
            }
            
            // Add ID parameter
            var idProp = entityType.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name.Equals("Id", System.StringComparison.OrdinalIgnoreCase));
            if (idProp != null)
            {
                sb.AppendLine("var paramId = __repoCmd__.CreateParameter()!;");
                sb.AppendLine("paramId.ParameterName = \"@Id\";");
                sb.AppendLine($"paramId.Value = {entityParam.Name}.Id;");
                sb.AppendLine("__repoCmd__.Parameters.Add(paramId);");
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateParameterBasedUpdate(IndentedStringBuilder sb, IMethodSymbol method, 
        string tableName, SqlDefine sqlDefine, bool isAsync)
    {
        sb.AppendLine("// Parameter-based update");
        var updateParams = method.Parameters.Where(p => 
            p.Type.Name != "CancellationToken" && 
            !p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase)).ToList();
        
        var idParam = method.Parameters.FirstOrDefault(p => 
            p.Name.Equals("id", System.StringComparison.OrdinalIgnoreCase));

        if (updateParams.Any() && idParam != null)
        {
            var setClause = string.Join(", ", updateParams.Select(p => $"{p.Name} = @{p.Name}"));
            sb.AppendLine($"__repoCmd__.CommandText = \"UPDATE {tableName} SET {setClause} WHERE Id = @id\";");
            
            foreach (var param in updateParams.Concat(new[] { idParam }))
            {
                GenerateParameterCode(sb, param);
            }
        }
        
        GenerateCommandExecution(sb, isAsync, method);
    }

    private void GenerateParameterCode(IndentedStringBuilder sb, IParameterSymbol param)
    {
        sb.AppendLine($"var param{param.Name} = __repoCmd__.CreateParameter()!;");
        sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
        sb.AppendLine($"param{param.Name}.Value = (object){param.Name} ?? (object)global::System.DBNull.Value;");
        sb.AppendLine($"__repoCmd__.Parameters.Add(param{param.Name});");
        sb.AppendLine();
    }

    private void GenerateCommandExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method, string itemPrefix = "")
    {
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine("__repoResult__ = __repoCmd__.ExecuteNonQuery();");
        }
        else
        {
            sb.AppendLine("__repoResult__ = __repoCmd__.ExecuteNonQuery();");
        }
    }

    private void GenerateMethodCompletion(IndentedStringBuilder sb, IMethodSymbol method, string methodName)
    {
        sb.AppendLine($"OnExecuted(\"{methodName}\", __repoCmd__, __repoResult__, System.Diagnostics.Stopwatch.GetTimestamp() - __repoStartTime__);");
        
        if (!method.ReturnsVoid)
        {
            // Note: Return statement is handled by CodeGenerationService
        }
    }


    private bool IsNumericType(ITypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_Int32 ||
               type.SpecialType == SpecialType.System_Int64 ||
               type.SpecialType == SpecialType.System_Decimal ||
               type.SpecialType == SpecialType.System_Double ||
               type.SpecialType == SpecialType.System_Single;
    }

    private System.Collections.Generic.List<IPropertySymbol> GetUpdatableProperties(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && 
                       p.GetMethod != null && 
                       p.SetMethod != null &&
                       p.Name != "Id" && 
                       p.Name != "EqualityContract")
            .ToList();
    }
}
