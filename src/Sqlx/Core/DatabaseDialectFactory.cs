// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactory.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;
using System;
using System.Collections.Concurrent;

namespace Sqlx.Core;

/// <summary>
/// Factory for creating database dialect providers.
/// </summary>
internal static class DatabaseDialectFactory
{
    private static readonly ConcurrentDictionary<SqlDefineTypes, IDatabaseDialectProvider> _dialectCache
        = new ConcurrentDictionary<SqlDefineTypes, IDatabaseDialectProvider>();

    /// <summary>
    /// Gets the dialect provider for the specified database type.
    /// </summary>
    /// <param name="dialectType">The database dialect type.</param>
    /// <returns>The appropriate dialect provider.</returns>
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefineTypes dialectType)
    {
        return _dialectCache.GetOrAdd(dialectType, CreateDialectProvider);
    }

    /// <summary>
    /// Gets the dialect provider for the specified SQL definition.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <returns>The appropriate dialect provider.</returns>
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefine sqlDefine)
    {
        // Try to match the SqlDefine to a known dialect type
        var dialectType = sqlDefine switch
        {
            var d when d.Equals(SqlDefine.MySql) => SqlDefineTypes.MySql,
            var d when d.Equals(SqlDefine.PgSql) => SqlDefineTypes.Postgresql,
            var d when d.Equals(SqlDefine.Oracle) => SqlDefineTypes.Oracle, // Will throw exception
            var d when d.Equals(SqlDefine.DB2) => SqlDefineTypes.DB2, // Will throw exception
            // SQLite has unique identifier (@sqlite) to distinguish from SqlServer
            var d when d.Equals(SqlDefine.SQLite) => SqlDefineTypes.SQLite,
            // SqlServer uses standard @ prefix
            var d when d.Equals(SqlDefine.SqlServer) => SqlDefineTypes.SqlServer,
            // For custom SqlDefine instances, check specific characteristics
            var d when d.ColumnLeft == "[" && d.ColumnRight == "]" && d.ParameterPrefix == "@sqlite" => SqlDefineTypes.SQLite,
            var d when d.ColumnLeft == "[" && d.ColumnRight == "]" && d.ParameterPrefix == "@" => SqlDefineTypes.SqlServer,
            _ => SqlDefineTypes.SqlServer // Default fallback
        };

        return GetDialectProvider(dialectType);
    }

    /// <summary>
    /// Creates a dialect provider for the specified type.
    /// </summary>
    /// <param name="dialectType">The dialect type.</param>
    /// <returns>The created dialect provider.</returns>
    private static IDatabaseDialectProvider CreateDialectProvider(SqlDefineTypes dialectType)
    {
        return dialectType switch
        {
            SqlDefineTypes.MySql => new MySqlDialectProvider(),
            SqlDefineTypes.SqlServer => new SqlServerDialectProvider(),
            SqlDefineTypes.Postgresql => new PostgreSqlDialectProvider(),
            SqlDefineTypes.SQLite => new SQLiteDialectProvider(),
            SqlDefineTypes.Oracle => throw new NotSupportedException("Oracle dialect support has been removed to reduce complexity. Use PostgreSQL or SQL Server instead."),
            SqlDefineTypes.DB2 => throw new NotSupportedException("DB2 dialect support has been removed to reduce complexity. Use PostgreSQL or SQL Server instead."),
            _ => throw new NotSupportedException($"Database dialect {dialectType} is not supported.")
        };
    }

    /// <summary>
    /// Registers a custom dialect provider.
    /// </summary>
    /// <param name="dialectType">The dialect type.</param>
    /// <param name="provider">The dialect provider.</param>
    public static void RegisterDialectProvider(SqlDefineTypes dialectType, IDatabaseDialectProvider provider)
    {
        _dialectCache.AddOrUpdate(dialectType, provider, (key, oldValue) => provider);
    }

    /// <summary>
    /// Clears all cached dialect providers.
    /// </summary>
    public static void ClearCache()
    {
        _dialectCache.Clear();
    }
}

