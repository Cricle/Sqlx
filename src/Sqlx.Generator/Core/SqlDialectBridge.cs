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
public readonly record struct SqlDefine(string ColumnLeft, string ColumnRight, string StringLeft, string StringRight, string ParameterPrefix)
{
    /// <summary>Wraps a column name with dialect-specific delimiters.</summary>
    public string WrapColumn(string columnName) =>
        string.IsNullOrEmpty(columnName) ? "" : $"{ColumnLeft}{columnName}{ColumnRight}";

    /// <summary>Wraps a string value with dialect-specific delimiters.</summary>
    public string WrapString(string value) =>
        value == null ? "NULL" : $"{StringLeft}{EscapeString(value)}{StringRight}";

    /// <summary>Escapes special characters in string values.</summary>
    private string EscapeString(string value) => value.Replace(StringLeft, StringLeft + StringLeft);

    /// <summary>Gets database type name - uses explicit assignment to handle SQLite/SQL Server overlap</summary>
    public string DatabaseType
    {
        get
        {
            // Explicit instance checks first (most reliable)
            if (this.Equals(SQLite)) return "SQLite";
            if (this.Equals(SqlServer)) return "SqlServer";
            if (this.Equals(MySql)) return "MySql";
            if (this.Equals(PostgreSql)) return "PostgreSql";
            if (this.Equals(Oracle)) return "Oracle";
            if (this.Equals(DB2)) return "DB2";
            
            // Pattern matching fallback
            return (ColumnLeft, ColumnRight, ParameterPrefix) switch
            {
                ("`", "`", "@") => "MySql",
                ("\"", "\"", "$") => "PostgreSql",
                ("\"", "\"", ":") => "Oracle",
                ("\"", "\"", "?") => "DB2",
                ("[", "]", "@") => "SqlServer",  // Default for [, ], @
                _ => "Unknown"
            };
        }
    }

    // Predefined dialect instances
    /// <summary>MySQL数据库方言配置</summary>
    public static readonly SqlDefine MySql = new("`", "`", "'", "'", "@");
    /// <summary>SQL Server数据库方言配置</summary>
    public static readonly SqlDefine SqlServer = new("[", "]", "'", "'", "@");
    /// <summary>PostgreSQL数据库方言配置</summary>
    public static readonly SqlDefine PostgreSql = new("\"", "\"", "'", "'", "$");
    /// <summary>SQLite数据库方言配置 - Uses @ like SQL Server, distinguished by explicit instance checks</summary>
    public static readonly SqlDefine SQLite = new("[", "]", "'", "'", "@");
    /// <summary>PostgreSQL数据库方言配置（别名）</summary>
    public static readonly SqlDefine PgSql = PostgreSql;
    /// <summary>Oracle数据库方言配置</summary>
    public static readonly SqlDefine Oracle = new("\"", "\"", "'", "'", ":");
    /// <summary>DB2数据库方言配置</summary>
    public static readonly SqlDefine DB2 = new("\"", "\"", "'", "'", "?");
}

