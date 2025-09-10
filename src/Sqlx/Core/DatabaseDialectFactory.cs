// -----------------------------------------------------------------------
// <copyright file="DatabaseDialectFactory.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;

namespace Sqlx.Core;

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
    public static IDatabaseDialectProvider GetDialectProvider(SqlDefineTypes dialectType)
    {
        return CreateDialectProvider(dialectType);
    }


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
        if (sqlDefine.Equals(SqlDefine.PgSql)) return GetDialectProvider(SqlDefineTypes.Postgresql);
        if (sqlDefine.Equals(SqlDefine.SQLite)) return GetDialectProvider(SqlDefineTypes.SQLite);
        if (sqlDefine.Equals(SqlDefine.Oracle)) return GetDialectProvider(SqlDefineTypes.Oracle);
        if (sqlDefine.Equals(SqlDefine.DB2)) return GetDialectProvider(SqlDefineTypes.DB2);

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
        return (sqlDefine.ColumnLeft, sqlDefine.ColumnRight, sqlDefine.ParameterPrefix) switch
        {
            ("`", "`", "@") => SqlDefineTypes.MySql,
            ("\"", "\"", "$") => SqlDefineTypes.Postgresql,
            ("\"", "\"", ":") => SqlDefineTypes.Oracle,
            ("\"", "\"", "?") => SqlDefineTypes.DB2,
            ("[", "]", "@sqlite") => SqlDefineTypes.SQLite,
            ("[", "]", "@") => SqlDefineTypes.SqlServer,
            _ => SqlDefineTypes.SqlServer // Default fallback
        };
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
            SqlDefineTypes.Oracle => throw new UnsupportedDialectException("Oracle (support removed to reduce complexity - use PostgreSQL instead)"),
            SqlDefineTypes.DB2 => throw new UnsupportedDialectException("DB2 (support removed to reduce complexity - use PostgreSQL instead)"),
            _ => throw new UnsupportedDialectException(dialectType.ToString())
        };
    }

    /// <summary>
    /// Clears all cached dialect providers (no-op for compatibility).
    /// </summary>
    public static void ClearCache()
    {
        // No-op: this factory doesn't cache
    }
}

