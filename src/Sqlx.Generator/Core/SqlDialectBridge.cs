// -----------------------------------------------------------------------
// <copyright file="SqlDialectBridge.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

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

    // Predefined dialect instances
    public static readonly SqlDefine MySql = new("`", "`", "'", "'", "@");
    public static readonly SqlDefine SqlServer = new("[", "]", "'", "'", "@");
    public static readonly SqlDefine PostgreSql = new("\"", "\"", "'", "'", "$");
    public static readonly SqlDefine SQLite = new("[", "]", "'", "'", "$");
    public static readonly SqlDefine PgSql = PostgreSql;
    public static readonly SqlDefine Oracle = new("\"", "\"", "'", "'", ":");
    public static readonly SqlDefine DB2 = new("\"", "\"", "'", "'", "?");
}

