// -----------------------------------------------------------------------
// <copyright file="OptimizedSqlBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Simplified SQL builder for code generation.
/// </summary>
public static class OptimizedSqlBuilder
{
    /// <summary>
    /// Builds a SELECT statement for the given parameters.
    /// </summary>
    public static string BuildSelect(string tableName, IEnumerable<string>? columns = null, IEnumerable<string>? whereColumns = null, int? limit = null)
    {
        var columnList = columns?.Any() == true ? string.Join(", ", columns) : "*";
        var sql = $"SELECT {columnList} FROM {tableName}";

        if (whereColumns?.Any() == true)
        {
            sql += " WHERE " + string.Join(" AND ", whereColumns.Select(col => $"{col} = @{col}"));
        }

        if (limit.HasValue)
        {
            sql += $" LIMIT {limit.Value}";
        }

        return sql;
    }

    /// <summary>
    /// Builds an INSERT statement for the given parameters.
    /// </summary>
    public static string BuildInsert(string tableName, IEnumerable<string> columns)
    {
        var columnList = columns.ToList();
        var columnNames = string.Join(", ", columnList);
        var paramNames = string.Join(", ", columnList.Select(col => $"@{col}"));
        return $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})";
    }

    /// <summary>
    /// Builds an UPDATE statement for the given parameters.
    /// </summary>
    public static string BuildUpdate(string tableName, IEnumerable<string> setColumns, IEnumerable<string>? whereColumns = null)
    {
        var setList = setColumns.ToList();
        var whereList = whereColumns?.ToList() ?? new List<string> { "id" };
        var setClauses = string.Join(", ", setList.Select(col => $"{col} = @{col}"));
        var whereClauses = string.Join(" AND ", whereList.Select(col => $"{col} = @{col}"));
        return $"UPDATE {tableName} SET {setClauses} WHERE {whereClauses}";
    }

    /// <summary>
    /// Builds a DELETE statement for the given parameters.
    /// </summary>
    public static string BuildDelete(string tableName, IEnumerable<string>? whereColumns = null)
    {
        var whereList = whereColumns?.ToList() ?? new List<string> { "id" };
        var whereClauses = string.Join(" AND ", whereList.Select(col => $"{col} = @{col}"));
        return $"DELETE FROM {tableName} WHERE {whereClauses}";
    }

    /// <summary>
    /// Extracts column names from entity type properties.
    /// </summary>
    public static IEnumerable<string> GetEntityColumns(INamedTypeSymbol entityType)
    {
        return entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
            .Select(p => p.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Extracts parameter names from method parameters.
    /// </summary>
    public static IEnumerable<string> GetMethodParameters(IMethodSymbol method)
    {
        return method.Parameters
            .Where(p => p.Type.Name != "CancellationToken")
            .Select(p => p.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Gets a table name from entity type (with fallback to type name).
    /// </summary>
    public static string GetTableName(INamedTypeSymbol? entityType, string? explicitTableName = null)
    {
        if (!string.IsNullOrEmpty(explicitTableName))
            return explicitTableName!;

        if (entityType == null)
            return "UnknownTable";

        // Check for TableName attribute
        var tableNameAttr = entityType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableNameAttribute" || a.AttributeClass?.Name == "TableAttribute");

        if (tableNameAttr?.ConstructorArguments.Length > 0 &&
            tableNameAttr.ConstructorArguments[0].Value is string tableName)
        {
            return tableName;
        }

        // Pluralize entity name as fallback
        var entityName = entityType.Name.ToLowerInvariant();
        return entityName.EndsWith("s") ? entityName : entityName + "s";
    }

}
