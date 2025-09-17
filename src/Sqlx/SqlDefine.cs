// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
    /// <summary>
    /// SQL dialect definitions for different database systems.
    /// </summary>
    public static class SqlDefine
    {
        /// <summary>MySQL: `column` with @ parameters</summary>
        public static readonly SqlDialect MySql = new("`", "`", "'", "'", "@");

        /// <summary>SQL Server: [column] with @ parameters</summary>
        public static readonly SqlDialect SqlServer = new("[", "]", "'", "'", "@");

        /// <summary>PostgreSQL: "column" with $ parameters</summary>
        public static readonly SqlDialect PgSql = new("\"", "\"", "'", "'", "$");

        /// <summary>Oracle: "column" with : parameters</summary>
        public static readonly SqlDialect Oracle = new("\"", "\"", "'", "'", ":");

        /// <summary>DB2: "column" with ? parameters</summary>
        public static readonly SqlDialect DB2 = new("\"", "\"", "'", "'", "?");

        /// <summary>SQLite: [column] with $ parameters (distinct from SQL Server)</summary>
        public static readonly SqlDialect Sqlite = new("[", "]", "'", "'", "$");
    }

    /// <summary>
    /// Represents a SQL dialect configuration.
    /// </summary>
    public readonly record struct SqlDialect(
        string ColumnLeft,
        string ColumnRight,
        string StringLeft,
        string StringRight,
        string ParameterPrefix)
    {
        /// <summary>Wraps a column name with dialect-specific delimiters.</summary>
        public string WrapColumn(string columnName) => columnName == null ? "" : $"{ColumnLeft}{columnName}{ColumnRight}";

        /// <summary>Wraps a string value with dialect-specific delimiters.</summary>
        public string WrapString(string value) => value == null ? "NULL" : $"{StringLeft}{value}{StringRight}";

        /// <summary>Creates a parameter with dialect-specific prefix.</summary>
        public string CreateParameter(string name) => $"{ParameterPrefix}{name}";

        /// <summary>Gets the database type based on dialect characteristics.</summary>
        public string DatabaseType => (ColumnLeft, ColumnRight, ParameterPrefix) switch
        {
            ("`", "`", "@") => "MySql",
            ("[", "]", "@") => "SqlServer",
            ("[", "]", "$") => "SQLite",
            ("\"", "\"", "$") => "PostgreSql",
            ("\"", "\"", ":") => "Oracle",
            ("\"", "\"", "?") => "DB2",
            _ => "Unknown"
        };
    }
}
