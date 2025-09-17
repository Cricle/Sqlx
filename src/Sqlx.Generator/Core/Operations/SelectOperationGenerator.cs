// -----------------------------------------------------------------------
// <copyright file="SelectOperationGenerator.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core.Operations;

/// <summary>
/// Generator for SELECT operations.
/// </summary>
public class SelectOperationGenerator : BaseOperationGenerator
{
    /// <inheritdoc/>
    public override string OperationName => "Select";

    /// <inheritdoc/>
    public override bool CanHandle(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("get") || 
               methodName.Contains("find") || 
               methodName.Contains("select") ||
               methodName.Contains("query") ||
               methodName.Contains("list") ||
               IsScalarMethod(method);
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

        // Determine the type of select operation
        if (IsScalarMethod(method))
        {
            GenerateScalarSelect(sb, method, tableName, isAsync);
        }
        else if (IsSingleResultMethod(method))
        {
            GenerateSingleSelect(sb, method, entityType, tableName, isAsync);
        }
        else
        {
            GenerateCollectionSelect(sb, method, entityType, tableName, isAsync);
        }

        // Generate method completion
        GenerateMethodCompletion(sb, method, methodName, isAsync);
    }

    private bool IsScalarMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("count") || 
               methodName.Contains("exists") ||
               methodName.Contains("sum") ||
               methodName.Contains("max") ||
               methodName.Contains("min") ||
               methodName.Contains("average") ||
               IsScalarReturnType(method.ReturnType, IsAsyncMethod(method));
    }

    private bool IsSingleResultMethod(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        return methodName.Contains("first") ||
               methodName.Contains("single") ||
               methodName.EndsWith("byid") ||
               (!IsCollectionReturnType(method.ReturnType) && !IsScalarMethod(method));
    }

    private bool IsAsyncMethod(IMethodSymbol method)
    {
        return method.ReturnType.Name == "Task" || 
               (method.ReturnType is INamedTypeSymbol taskType && taskType.Name == "Task");
    }

    private void GenerateScalarSelect(IndentedStringBuilder sb, IMethodSymbol method, 
        string tableName, bool isAsync)
    {
        sb.AppendLine("// 🚀 智能标量查询 - Count, Exists, Sum, etc.");
        
        var methodNameLower = method.Name.ToLowerInvariant();
        var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();

        if (methodNameLower.Contains("count"))
        {
            GenerateCountQuery(sb, tableName, whereParams);
        }
        else if (methodNameLower.Contains("exists"))
        {
            GenerateExistsQuery(sb, tableName, whereParams);
        }
        else if (methodNameLower.Contains("sum"))
        {
            GenerateSumQuery(sb, tableName, whereParams, method);
        }
        else if (methodNameLower.Contains("max"))
        {
            GenerateMaxQuery(sb, tableName, whereParams, method);
        }
        else if (methodNameLower.Contains("min"))
        {
            GenerateMinQuery(sb, tableName, whereParams, method);
        }
        else if (methodNameLower.Contains("average"))
        {
            GenerateAverageQuery(sb, tableName, whereParams, method);
        }
        else
        {
            // Generic scalar query
            GenerateGenericScalarQuery(sb, tableName, whereParams, method);
        }

        GenerateWhereClause(sb, whereParams);
        GenerateScalarExecution(sb, isAsync, method);
    }

    private void GenerateSingleSelect(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        sb.AppendLine("// 🚀 智能单实体查询");
        
        var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
        
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT * FROM {tableName}\";");
        
        if (whereParams.Any())
        {
            GenerateWhereClause(sb, whereParams);
        }
        
        sb.AppendLine("__repoCmd__.CommandText += \" LIMIT 1\";");
        
        GenerateSingleEntityExecution(sb, isAsync, method, entityType);
    }

    private void GenerateCollectionSelect(IndentedStringBuilder sb, IMethodSymbol method, 
        INamedTypeSymbol? entityType, string tableName, bool isAsync)
    {
        sb.AppendLine("// 🚀 智能集合查询");
        
        var whereParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
        
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT * FROM {tableName}\";");
        
        if (whereParams.Any())
        {
            GenerateWhereClause(sb, whereParams);
        }
        
        GenerateCollectionExecution(sb, isAsync, method, entityType);
    }

    private void GenerateCountQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams)
    {
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT COUNT(*) FROM {tableName}\";");
    }

    private void GenerateExistsQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams)
    {
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName}\";");
        if (whereParams.Any())
        {
            // WHERE clause will be added by GenerateWhereClause
            sb.AppendLine("__repoCmd__.CommandText += \"\";");
        }
        sb.AppendLine("__repoCmd__.CommandText += \") THEN 1 ELSE 0 END\";");
    }

    private void GenerateSumQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams, IMethodSymbol method)
    {
        var column = InferColumnFromMethodName(method.Name, "sum");
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT SUM({column}) FROM {tableName}\";");
    }

    private void GenerateMaxQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams, IMethodSymbol method)
    {
        var column = InferColumnFromMethodName(method.Name, "max");
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT MAX({column}) FROM {tableName}\";");
    }

    private void GenerateMinQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams, IMethodSymbol method)
    {
        var column = InferColumnFromMethodName(method.Name, "min");
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT MIN({column}) FROM {tableName}\";");
    }

    private void GenerateAverageQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams, IMethodSymbol method)
    {
        var column = InferColumnFromMethodName(method.Name, "average");
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT AVG({column}) FROM {tableName}\";");
    }

    private void GenerateGenericScalarQuery(IndentedStringBuilder sb, string tableName, 
        System.Collections.Generic.List<IParameterSymbol> whereParams, IMethodSymbol method)
    {
        sb.AppendLine($"__repoCmd__.CommandText = \"SELECT * FROM {tableName}\";");
    }

    private void GenerateWhereClause(IndentedStringBuilder sb, 
        System.Collections.Generic.List<IParameterSymbol> whereParams)
    {
        if (whereParams.Any())
        {
            var conditions = whereParams.Select(p => $"{InferColumnNameFromParameter(p.Name)} = @{p.Name}");
            var whereClause = string.Join(" AND ", conditions);
            sb.AppendLine($"__repoCmd__.CommandText += \" WHERE {whereClause}\";");
            
            foreach (var param in whereParams)
            {
                sb.AppendLine($"var param{param.Name} = __repoCmd__.CreateParameter()!;");
                sb.AppendLine($"param{param.Name}.ParameterName = \"@{param.Name}\";");
                sb.AppendLine($"param{param.Name}.Value = (object){param.Name} ?? (object)global::System.DBNull.Value;");
                sb.AppendLine($"param{param.Name}.DbType = {GetDbTypeForParameter(param)};");
                sb.AppendLine($"__repoCmd__.Parameters.Add(param{param.Name});");
            }
            sb.AppendLine();
        }
    }

    private void GenerateScalarExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method)
    {
        if (isAsync)
        {
            var cancellationToken = GetCancellationTokenParameter(method);
            sb.AppendLine("var scalarResult = __repoCmd__.ExecuteScalar();");
        }
        else
        {
            sb.AppendLine("var scalarResult = __repoCmd__.ExecuteScalar();");
        }
        
        // Convert result based on return type
        var returnType = method.ReturnType;
        if (IsAsyncMethod(method) && returnType is INamedTypeSymbol taskType && taskType.TypeArguments.Length > 0)
        {
            returnType = taskType.TypeArguments[0];
        }
        
        GenerateScalarConversion(sb, returnType);
    }

    private void GenerateSingleEntityExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        sb.AppendLine("using var reader = __repoCmd__.ExecuteReader();");
        
        sb.AppendLine("if (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType, "result");
        }
        else
        {
            sb.AppendLine("// Entity mapping would go here");
            sb.AppendLine("__repoResult__ = default;");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__repoResult__ = default;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateCollectionExecution(IndentedStringBuilder sb, bool isAsync, IMethodSymbol method, INamedTypeSymbol? entityType)
    {
        sb.AppendLine("using var reader = __repoCmd__.ExecuteReader();");
        
        sb.AppendLine("var results = new global::System.Collections.Generic.List<" + (entityType?.Name ?? "object") + ">();");
        sb.AppendLine("while (reader.Read())");
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (entityType != null)
        {
            GenerateEntityMapping(sb, entityType, "item");
            sb.AppendLine("results.Add(item);");
        }
        else
        {
            sb.AppendLine("// Entity mapping would go here");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("__repoResult__ = results;");
    }

    private void GenerateEntityMapping(IndentedStringBuilder sb, INamedTypeSymbol entityType, string variableName)
    {
        sb.AppendLine($"var {variableName} = new {entityType.ToDisplayString()}()!;");
        
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.SetMethod != null)
            .ToList();
        
        foreach (var prop in properties)
        {
            sb.AppendLine($"if (reader[\"{prop.Name}\"] != global::System.DBNull.Value)");
            sb.AppendLine("{");
            sb.PushIndent();
            sb.AppendLine($"{variableName}.{prop.Name} = ({prop.Type.ToDisplayString()})reader[\"{prop.Name}\"];");
            sb.PopIndent();
            sb.AppendLine("}");
        }
    }

    private void GenerateScalarConversion(IndentedStringBuilder sb, ITypeSymbol returnType)
    {
        sb.AppendLine("if (scalarResult != null && scalarResult != global::System.DBNull.Value)");
        sb.AppendLine("{");
        sb.PushIndent();
        
        if (returnType.SpecialType == SpecialType.System_Int32)
        {
            sb.AppendLine("__repoResult__ = global::System.Convert.ToInt32(scalarResult);");
        }
        else if (returnType.SpecialType == SpecialType.System_Boolean)
        {
            sb.AppendLine("__repoResult__ = global::System.Convert.ToBoolean(scalarResult);");
        }
        else if (returnType.SpecialType == SpecialType.System_Int64)
        {
            sb.AppendLine("__repoResult__ = global::System.Convert.ToInt64(scalarResult);");
        }
        else
        {
            sb.AppendLine($"__repoResult__ = ({returnType.ToDisplayString()})scalarResult;");
        }
        
        sb.PopIndent();
        sb.AppendLine("}");
        sb.AppendLine("else");
        sb.AppendLine("{");
        sb.PushIndent();
        sb.AppendLine("__repoResult__ = default;");
        sb.PopIndent();
        sb.AppendLine("}");
    }

    private void GenerateMethodCompletion(IndentedStringBuilder sb, IMethodSymbol method, string methodName, bool isAsync)
    {
        sb.AppendLine($"OnExecuted(\"{methodName}\", __repoCmd__, __repoResult__, System.Diagnostics.Stopwatch.GetTimestamp() - __repoStartTime__);");
        // Note: Return statement is handled by CodeGenerationService
    }

    private string InferColumnFromMethodName(string methodName, string operation)
    {
        var operationIndex = methodName.ToLowerInvariant().IndexOf(operation.ToLowerInvariant());
        if (operationIndex > 0)
        {
            var prefix = methodName.Substring(0, operationIndex);
            if (prefix.Length > 0)
            {
                return prefix;
            }
        }
        return "Id"; // Default column
    }

    private string InferColumnNameFromParameter(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;
        
        return char.ToUpperInvariant(parameterName[0]) + parameterName.Substring(1);
    }

    private bool IsScalarReturnType(ITypeSymbol type, bool isAsync)
    {
        if (isAsync && type is INamedTypeSymbol taskType && taskType.TypeArguments.Length > 0)
        {
            type = taskType.TypeArguments[0];
        }
        
        return type.SpecialType == SpecialType.System_Int32 ||
               type.SpecialType == SpecialType.System_Boolean ||
               type.SpecialType == SpecialType.System_Int64 ||
               type.SpecialType == SpecialType.System_Decimal ||
               type.SpecialType == SpecialType.System_Double;
    }

    private bool IsCollectionReturnType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.AllInterfaces.Any(i => 
                i.Name == "IEnumerable" || 
                i.Name == "ICollection" || 
                i.Name == "IList");
        }
        return false;
    }

    private string GetDbTypeForParameter(IParameterSymbol parameter)
    {
        return parameter.Type.SpecialType switch
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
