// -----------------------------------------------------------------------
// <copyright file="DialectHelper.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Sqlx.Generator.Core;

/// <summary>
/// Helper methods for extracting dialect and table name information from attributes.
/// </summary>
internal static class DialectHelper
{
    /// <summary>
    /// Extracts the dialect from RepositoryFor attribute.
    /// </summary>
    /// <param name="repositoryClass">The repository class symbol.</param>
    /// <returns>The SQL dialect type, or SQLite as default.</returns>
    public static SqlDefineTypes GetDialectFromRepositoryFor(INamedTypeSymbol repositoryClass)
    {
        var attr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "RepositoryForAttribute" ||
                (attr.AttributeClass?.Name.StartsWith("RepositoryForAttribute`") ?? false));

        if (attr != null)
        {
            var dialectArg = attr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Dialect");

            if (dialectArg.Value.Value is int dialectValue)
            {
                return (SqlDefineTypes)dialectValue;
            }
        }

        return SqlDefineTypes.SQLite; // default
    }

    /// <summary>
    /// Extracts the table name from RepositoryFor attribute.
    /// Priority: RepositoryFor.TableName > TableNameAttribute > inferred from entity type.
    /// </summary>
    /// <param name="repositoryClass">The repository class symbol.</param>
    /// <param name="entityType">The entity type (for fallback).</param>
    /// <returns>The table name, or null if not found.</returns>
    public static string? GetTableNameFromRepositoryFor(INamedTypeSymbol repositoryClass, INamedTypeSymbol? entityType)
    {
        // First priority: Check RepositoryFor attribute's TableName property
        var repositoryForAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr =>
                attr.AttributeClass?.Name == "RepositoryForAttribute" ||
                (attr.AttributeClass?.Name.StartsWith("RepositoryForAttribute`") ?? false));

        if (repositoryForAttr != null)
        {
            // Look for TableName named argument
            var tableNameArg = repositoryForAttr.NamedArguments
                .FirstOrDefault(arg => arg.Key == "TableName");

            if (tableNameArg.Value.Value is string tableNameValue && !string.IsNullOrEmpty(tableNameValue))
            {
                return tableNameValue;
            }
        }

        // Second priority: Check if repository class has TableNameAttribute
        var repositoryTableNameAttr = repositoryClass.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute" ||
                                   attr.AttributeClass?.Name == "TableName");

        if (repositoryTableNameAttr != null && repositoryTableNameAttr.ConstructorArguments.Length > 0)
        {
            var tableName = repositoryTableNameAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(tableName))
                return tableName;
        }

        // Third priority: Check if entity type has TableNameAttribute
        if (entityType != null)
        {
            var entityTableNameAttr = entityType.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "TableNameAttribute" ||
                                       attr.AttributeClass?.Name == "TableName");

            if (entityTableNameAttr != null && entityTableNameAttr.ConstructorArguments.Length > 0)
            {
                var tableName = entityTableNameAttr.ConstructorArguments[0].Value?.ToString();
                if (!string.IsNullOrEmpty(tableName))
                    return tableName;
            }
        }

        // Fallback: infer from entity type name (convert to lowercase, keep plural if present)
        if (entityType != null)
        {
            return entityType.Name.ToLowerInvariant();
        }

        // Last resort: use repository class name
        return repositoryClass.Name.Replace("Repository", "").ToLowerInvariant();
    }

    /// <summary>
    /// Gets the dialect provider for the specified dialect type.
    /// </summary>
    /// <param name="dialect">The SQL dialect type.</param>
    /// <returns>The dialect provider instance.</returns>
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefineTypes dialect)
    {
        return dialect switch
        {
            SqlDefineTypes.PostgreSql => new PostgreSqlDialectProvider(),
            SqlDefineTypes.MySql => new MySqlDialectProvider(),
            SqlDefineTypes.SqlServer => new SqlServerDialectProvider(),
            SqlDefineTypes.SQLite => new SQLiteDialectProvider(),
            SqlDefineTypes.Oracle => new SQLiteDialectProvider(), // TODO: Implement Oracle
            SqlDefineTypes.DB2 => new SQLiteDialectProvider(), // TODO: Implement DB2
            _ => new SQLiteDialectProvider()
        };
    }
}

