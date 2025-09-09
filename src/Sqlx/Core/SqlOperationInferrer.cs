// -----------------------------------------------------------------------
// <copyright file="SqlOperationInferrer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Core;

/// <summary>
/// Infers SQL operations from method signatures and attributes.
/// </summary>
internal static class SqlOperationInferrer
{
    private static readonly Dictionary<string, SqlOperationType> _methodNamePatterns = new()
    {
        // SELECT patterns
        ["get"] = SqlOperationType.Select,
        ["find"] = SqlOperationType.Select,
        ["select"] = SqlOperationType.Select,
        ["query"] = SqlOperationType.Select,
        ["search"] = SqlOperationType.Select,
        ["list"] = SqlOperationType.Select,
        ["fetch"] = SqlOperationType.Select,
        
        // INSERT patterns
        ["create"] = SqlOperationType.Insert,
        ["insert"] = SqlOperationType.Insert,
        ["add"] = SqlOperationType.Insert,
        ["save"] = SqlOperationType.Insert,
        
        // UPDATE patterns
        ["update"] = SqlOperationType.Update,
        ["modify"] = SqlOperationType.Update,
        ["edit"] = SqlOperationType.Update,
        ["change"] = SqlOperationType.Update,
        
        // DELETE patterns
        ["delete"] = SqlOperationType.Delete,
        ["remove"] = SqlOperationType.Delete,
        ["destroy"] = SqlOperationType.Delete,
        
        // SCALAR patterns
        ["count"] = SqlOperationType.Scalar,
        ["exists"] = SqlOperationType.Scalar,
        ["sum"] = SqlOperationType.Scalar,
        ["avg"] = SqlOperationType.Scalar,
        ["min"] = SqlOperationType.Scalar,
        ["max"] = SqlOperationType.Scalar
    };

    /// <summary>
    /// Infers the SQL operation type from method signature.
    /// </summary>
    public static SqlOperationType InferOperation(IMethodSymbol method)
    {
        // First check for explicit attributes
        var sqlExecuteTypeAttr = method.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "SqlExecuteTypeAttribute");
        
        if (sqlExecuteTypeAttr?.ConstructorArguments.Length > 0)
        {
            var operationType = sqlExecuteTypeAttr.ConstructorArguments[0].Value;
            return operationType switch
            {
                0 => SqlOperationType.Select,
                1 => SqlOperationType.Update,
                2 => SqlOperationType.Insert,
                3 => SqlOperationType.Delete,
                _ => InferFromMethodSignature(method)
            };
        }

        return InferFromMethodSignature(method);
    }

    private static SqlOperationType InferFromMethodSignature(IMethodSymbol method)
    {
        var methodName = method.Name.ToLowerInvariant();
        var returnType = method.ReturnType;
        var isAsync = TypeAnalyzer.IsAsyncType(returnType);
        var actualReturnType = TypeAnalyzer.GetInnerType(returnType);

        // Check method name patterns
        foreach (var pattern in _methodNamePatterns)
        {
            if (methodName.Contains(pattern.Key))
            {
                return pattern.Value;
            }
        }

        // Infer from return type
        if (TypeAnalyzer.IsScalarReturnType(returnType, isAsync))
        {
            return SqlOperationType.Scalar;
        }

        if (TypeAnalyzer.IsCollectionType(actualReturnType))
        {
            return SqlOperationType.Select;
        }

        // Check parameters for entity types (likely INSERT/UPDATE)
        var hasEntityParameter = method.Parameters.Any(p => TypeAnalyzer.IsLikelyEntityType(p.Type));
        if (hasEntityParameter)
        {
            return actualReturnType.SpecialType == SpecialType.System_Int32 ? 
                SqlOperationType.Insert : SqlOperationType.Update;
        }

        // Default to SELECT for single entity returns
        return SqlOperationType.Select;
    }

    /// <summary>
    /// Generates SQL template for the operation.
    /// </summary>
    public static string GenerateSqlTemplate(SqlOperationType operation, string tableName, INamedTypeSymbol? entityType)
    {
        return operation switch
        {
            SqlOperationType.Select => $"SELECT * FROM [{tableName}]",
            SqlOperationType.Insert => GenerateInsertSql(tableName, entityType),
            SqlOperationType.Update => GenerateUpdateSql(tableName, entityType),
            SqlOperationType.Delete => $"DELETE FROM [{tableName}] WHERE [Id] = @id",
            SqlOperationType.Scalar => $"SELECT COUNT(*) FROM [{tableName}]",
            _ => $"SELECT * FROM [{tableName}]"
        };
    }

    private static string GenerateInsertSql(string tableName, INamedTypeSymbol? entityType)
    {
        if (entityType == null)
        {
            return $"INSERT INTO [{tableName}] VALUES (...)";
        }

        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsReadOnly && p.Name != "Id") // Exclude Id for auto-increment
            .ToArray();

        if (properties.Length == 0)
        {
            return $"INSERT INTO [{tableName}] DEFAULT VALUES";
        }

        var columns = string.Join(", ", properties.Select(p => $"[{p.Name}]"));
        var parameters = string.Join(", ", properties.Select(p => $"@{p.Name.ToLowerInvariant()}"));
        
        return $"INSERT INTO [{tableName}] ({columns}) VALUES ({parameters})";
    }

    private static string GenerateUpdateSql(string tableName, INamedTypeSymbol? entityType)
    {
        if (entityType == null)
        {
            return $"UPDATE [{tableName}] SET ... WHERE [Id] = @id";
        }

        var properties = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsReadOnly && p.Name != "Id")
            .ToArray();

        if (properties.Length == 0)
        {
            return $"UPDATE [{tableName}] SET [UpdatedAt] = GETDATE() WHERE [Id] = @id";
        }

        var setClause = string.Join(", ", properties.Select(p => $"[{p.Name}] = @{p.Name.ToLowerInvariant()}"));
        return $"UPDATE [{tableName}] SET {setClause} WHERE [Id] = @id";
    }
}

/// <summary>
/// SQL operation types.
/// </summary>
internal enum SqlOperationType
{
    Select,
    Insert,
    Update,
    Delete,
    Scalar
}
