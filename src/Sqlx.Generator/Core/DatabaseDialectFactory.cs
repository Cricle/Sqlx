// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactory.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx;

using System;

namespace Sqlx.Generator;

/// <summary>
/// Simple factory for creating database dialect providers.
/// No caching needed - these are lightweight objects.
/// </summary>
internal static class DatabaseDialectFactory
{
    /// <summary>
    /// Gets the dialect provider for the specified database type.
    /// </summary>
    /// <param name="dialectType">The database dialect type.</param>
    /// <returns>The appropriate dialect provider.</returns>
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefineTypes dialectType) => dialectType switch
    {
        SqlDefineTypes.MySql => new MySqlDialectProvider(),
        SqlDefineTypes.SqlServer => new SqlServerDialectProvider(),
        SqlDefineTypes.PostgreSql => new PostgreSqlDialectProvider(),
        SqlDefineTypes.SQLite => new SQLiteDialectProvider(),
        _ => throw new NotSupportedException($"Unsupported dialect: {dialectType}")
    };


    /// <summary>
    /// Gets the dialect provider for the specified SQL definition.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <returns>The appropriate dialect provider.</returns>
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefine sqlDefine)
    {
        // Direct equality checks for known static instances
        if (sqlDefine.Equals(SqlDefine.MySql)) return GetDialectProvider(SqlDefineTypes.MySql);
        if (sqlDefine.Equals(SqlDefine.SqlServer)) return GetDialectProvider(SqlDefineTypes.SqlServer);
        if (sqlDefine.Equals(SqlDefine.PostgreSql)) return GetDialectProvider(SqlDefineTypes.PostgreSql);
        if (sqlDefine.Equals(SqlDefine.SQLite)) return GetDialectProvider(SqlDefineTypes.SQLite);

        // For custom SqlDefine instances, infer from characteristics
        var dialectType = InferDialectFromCharacteristics(sqlDefine);
        return GetDialectProvider(dialectType);
    }

    /// <summary>
    /// Infers the dialect type from SqlDefine characteristics.
    /// </summary>
    /// <param name="sqlDefine">The SQL definition.</param>
    /// <returns>The inferred dialect type.</returns>
    private static SqlDefineTypes InferDialectFromCharacteristics(SqlDefine sqlDefine)
    {
        // Use DatabaseType property for accurate detection (handles SQLite/SQL Server overlap)
        return sqlDefine.DatabaseType switch
        {
            "MySql" => SqlDefineTypes.MySql,
            "SqlServer" => SqlDefineTypes.SqlServer,
            "PostgreSql" => SqlDefineTypes.PostgreSql,
            "SQLite" => SqlDefineTypes.SQLite,
            "Oracle" => SqlDefineTypes.Oracle,
            "DB2" => SqlDefineTypes.DB2,
            _ => throw new NotSupportedException($"Unsupported dialect: {sqlDefine.DatabaseType}")
        };
    }


}

