// -----------------------------------------------------------------------
// <copyright file="SqlDialectBridge.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

/// <summary>
/// Internal SQL dialect definition for code generation (simplified version).
/// This avoids circular dependencies while maintaining compatibility.
/// </summary>
/// <param name="ColumnLeft">Gets column left delimiter</param>
/// <param name="ColumnRight">Gets column right delimiter</param>
/// <param name="StringLeft">Gets string left delimiter</param>
/// <param name="StringRight">Gets string right delimiter</param>
/// <param name="ParameterPrefix">Gets parameter prefix</param>
/// <param name="DbTypeName">Gets database type name for disambiguation (SQLite vs SqlServer)</param>
public readonly record struct SqlDefine(string ColumnLeft, string ColumnRight, string StringLeft, string StringRight, string ParameterPrefix, string DbTypeName = "")
{
    /// <summary>Wraps a column name with dialect-specific delimiters.</summary>
    public string WrapColumn(string columnName) =>
        string.IsNullOrEmpty(columnName) ? "" : $"{ColumnLeft}{columnName}{ColumnRight}";

    /// <summary>Wraps a string value with dialect-specific delimiters.</summary>
    public string WrapString(string value) =>
        value == null ? "NULL" : $"{StringLeft}{EscapeString(value)}{StringRight}";

    /// <summary>Escapes special characters in string values.</summary>
    private string EscapeString(string value) => value.Replace(StringLeft, StringLeft + StringLeft);

    /// <summary>Gets database type name - uses DbTypeName field for explicit disambiguation</summary>
    public string DatabaseType
    {
        get
        {
            // If DbTypeName is explicitly set, use it (most reliable)
            if (!string.IsNullOrEmpty(DbTypeName))
                return DbTypeName;
            
            // Pattern matching fallback for backward compatibility
            return (ColumnLeft, ColumnRight, ParameterPrefix) switch
            {
                ("`", "`", "@") => "MySql",
                ("\"", "\"", "@") => "PostgreSql",  // PostgreSQL with @ (Npgsql named parameters)
                ("\"", "\"", "$") => "PostgreSql",  // PostgreSQL with $ (legacy/positional)
                ("\"", "\"", ":") => "Oracle",
                ("\"", "\"", "?") => "DB2",
                ("[", "]", "@") => "SqlServer",  // Default for [, ], @ when DbTypeName not set
                _ => "Unknown"
            };
        }
    }

    // Predefined dialect instances with explicit DbTypeName
    /// <summary>MySQL数据库方言配置</summary>
    public static readonly SqlDefine MySql = new("`", "`", "'", "'", "@", "MySql");
    /// <summary>SQL Server数据库方言配置</summary>
    public static readonly SqlDefine SqlServer = new("[", "]", "'", "'", "@", "SqlServer");
    /// <summary>PostgreSQL数据库方言配置 - Uses @ for named parameters (Npgsql standard)</summary>
    public static readonly SqlDefine PostgreSql = new("\"", "\"", "'", "'", "@", "PostgreSql");
    /// <summary>SQLite数据库方言配置 - Uses @ like SQL Server, distinguished by DbTypeName</summary>
    public static readonly SqlDefine SQLite = new("[", "]", "'", "'", "@", "SQLite");
    /// <summary>PostgreSQL数据库方言配置（别名）</summary>
    public static readonly SqlDefine PgSql = PostgreSql;
    /// <summary>Oracle数据库方言配置</summary>
    public static readonly SqlDefine Oracle = new("\"", "\"", "'", "'", ":", "Oracle");
    /// <summary>DB2数据库方言配置 - Uses :param format</summary>
    public static readonly SqlDefine DB2 = new("\"", "\"", "'", "'", ":", "DB2");
}

