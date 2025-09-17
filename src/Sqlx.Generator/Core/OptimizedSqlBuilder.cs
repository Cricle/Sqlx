// -----------------------------------------------------------------------
// <copyright file="OptimizedSqlBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Core;

/// <summary>
/// Optimized SQL builder that generates efficient SQL statements with minimal allocations.
/// </summary>
public static class OptimizedSqlBuilder
{
    private static readonly StringBuilder _sharedBuilder = new StringBuilder(1024);
    private static readonly Dictionary<string, string> _sqlCache = new Dictionary<string, string>();

    /// <summary>
    /// Builds a SELECT statement for the given parameters.
    /// </summary>
    public static string BuildSelect(string tableName, IEnumerable<string>? columns = null, IEnumerable<string>? whereColumns = null, int? limit = null)
    {
        var cacheKey = $"SELECT_{tableName}_{string.Join(",", columns ?? Enumerable.Empty<string>())}_{string.Join(",", whereColumns ?? Enumerable.Empty<string>())}_{limit}";

        if (_sqlCache.TryGetValue(cacheKey, out var cachedSql))
            return cachedSql;

        lock (_sharedBuilder)
        {
            _sharedBuilder.Clear();
            _sharedBuilder.Append("SELECT ");

            if (columns?.Any() == true)
            {
                _sharedBuilder.Append(string.Join(", ", columns));
            }
            else
            {
                _sharedBuilder.Append("*");
            }

            _sharedBuilder.Append(" FROM ").Append(tableName);

            if (whereColumns?.Any() == true)
            {
                _sharedBuilder.Append(" WHERE ");
                _sharedBuilder.Append(string.Join(" AND ", whereColumns.Select(col => $"{col} = @{col}")));
            }

            if (limit.HasValue)
            {
                _sharedBuilder.Append(" LIMIT ").Append(limit.Value);
            }

            var sql = _sharedBuilder.ToString();
            _sqlCache[cacheKey] = sql;
            return sql;
        }
    }

    /// <summary>
    /// Builds an INSERT statement for the given parameters.
    /// </summary>
    public static string BuildInsert(string tableName, IEnumerable<string> columns)
    {
        var columnList = columns.ToList();
        var cacheKey = $"INSERT_{tableName}_{string.Join(",", columnList)}";

        if (_sqlCache.TryGetValue(cacheKey, out var cachedSql))
            return cachedSql;

        lock (_sharedBuilder)
        {
            _sharedBuilder.Clear();
            _sharedBuilder.Append("INSERT INTO ").Append(tableName).Append(" (");
            _sharedBuilder.Append(string.Join(", ", columnList));
            _sharedBuilder.Append(") VALUES (");
            _sharedBuilder.Append(string.Join(", ", columnList.Select(col => $"@{col}")));
            _sharedBuilder.Append(")");

            var sql = _sharedBuilder.ToString();
            _sqlCache[cacheKey] = sql;
            return sql;
        }
    }

    /// <summary>
    /// Builds an UPDATE statement for the given parameters.
    /// </summary>
    public static string BuildUpdate(string tableName, IEnumerable<string> setColumns, IEnumerable<string>? whereColumns = null)
    {
        var setList = setColumns.ToList();
        var whereList = whereColumns?.ToList() ?? new List<string> { "id" };
        var cacheKey = $"UPDATE_{tableName}_{string.Join(",", setList)}_{string.Join(",", whereList)}";

        if (_sqlCache.TryGetValue(cacheKey, out var cachedSql))
            return cachedSql;

        lock (_sharedBuilder)
        {
            _sharedBuilder.Clear();
            _sharedBuilder.Append("UPDATE ").Append(tableName).Append(" SET ");
            _sharedBuilder.Append(string.Join(", ", setList.Select(col => $"{col} = @{col}")));
            _sharedBuilder.Append(" WHERE ");
            _sharedBuilder.Append(string.Join(" AND ", whereList.Select(col => $"{col} = @{col}")));

            var sql = _sharedBuilder.ToString();
            _sqlCache[cacheKey] = sql;
            return sql;
        }
    }

    /// <summary>
    /// Builds a DELETE statement for the given parameters.
    /// </summary>
    public static string BuildDelete(string tableName, IEnumerable<string>? whereColumns = null)
    {
        var whereList = whereColumns?.ToList() ?? new List<string> { "id" };
        var cacheKey = $"DELETE_{tableName}_{string.Join(",", whereList)}";

        if (_sqlCache.TryGetValue(cacheKey, out var cachedSql))
            return cachedSql;

        lock (_sharedBuilder)
        {
            _sharedBuilder.Clear();
            _sharedBuilder.Append("DELETE FROM ").Append(tableName).Append(" WHERE ");
            _sharedBuilder.Append(string.Join(" AND ", whereList.Select(col => $"{col} = @{col}")));

            var sql = _sharedBuilder.ToString();
            _sqlCache[cacheKey] = sql;
            return sql;
        }
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

    /// <summary>
    /// Clears the SQL cache (useful for testing or memory management).
    /// </summary>
    public static void ClearCache()
    {
        lock (_sqlCache)
        {
            _sqlCache.Clear();
        }
    }
}
