using System;

namespace Sqlx
{
    /// <summary>Provides predefined database dialect configurations for SQL generation.</summary>
    public static class SqlDefine
    {
        /// <summary>SQL Server dialect configuration - uses [brackets] for columns and @ for parameters.</summary>
        public static readonly SqlDialect SqlServer = new("[", "]", "'", "'", "@");

        /// <summary>MySQL dialect configuration - uses `backticks` for columns and @ for parameters.</summary>
        public static readonly SqlDialect MySql = new("`", "`", "'", "'", "@");

        /// <summary>PostgreSQL dialect configuration - uses "quotes" for columns and $ for parameters.</summary>
        public static readonly SqlDialect PostgreSql = new("\"", "\"", "'", "'", "$");

        /// <summary>SQLite dialect configuration - uses [brackets] for columns and @ for parameters (ADO.NET standard).</summary>
        public static readonly SqlDialect SQLite = new("[", "]", "'", "'", "@");

        /// <summary>Oracle dialect configuration - uses "quotes" for columns and : for parameters.</summary>
        public static readonly SqlDialect Oracle = new("\"", "\"", "'", "'", ":");

        /// <summary>DB2 dialect configuration - uses "quotes" for columns and ? for parameters.</summary>
        public static readonly SqlDialect DB2 = new("\"", "\"", "'", "'", "?");

        /// <summary>Alias for PostgreSQL - maintains backward compatibility.</summary>
        public static readonly SqlDialect PgSql = PostgreSql;

        /// <summary>Alias for SQLite - maintains backward compatibility.</summary>
        public static readonly SqlDialect Sqlite = SQLite;

        /// <summary>Gets the appropriate SQL dialect for the specified database type.</summary>
        /// <param name="databaseType">The database type to get the dialect for.</param>
        /// <returns>The corresponding SqlDialect instance.</returns>
        /// <exception cref="NotSupportedException">Thrown when the database type is not supported.</exception>
        public static SqlDialect GetDialect(Annotations.SqlDefineTypes databaseType) => databaseType switch
        {
            Annotations.SqlDefineTypes.MySql => MySql,
            Annotations.SqlDefineTypes.SqlServer => SqlServer,
            Annotations.SqlDefineTypes.PostgreSql => PostgreSql,
            Annotations.SqlDefineTypes.SQLite => SQLite,
            Annotations.SqlDefineTypes.Oracle => Oracle,
            Annotations.SqlDefineTypes.DB2 => DB2,
            _ => throw new NotSupportedException(databaseType.ToString())
        };
    }

    /// <summary>SQL dialect configuration for database-specific syntax</summary>
    public readonly record struct SqlDialect(
        string ColumnLeft,
        string ColumnRight,
        string StringLeft,
        string StringRight,
        string ParameterPrefix)
    {
        /// <summary>Wraps a column name with dialect-specific delimiters.</summary>
        public string WrapColumn(string columnName) =>
            string.IsNullOrEmpty(columnName) ? "" : $"{ColumnLeft}{columnName}{ColumnRight}";

        /// <summary>Wraps a string value with dialect-specific delimiters.</summary>
        public string WrapString(string value) =>
            value == null ? "NULL" : $"{StringLeft}{EscapeString(value)}{StringRight}";

        /// <summary>Creates a parameter with dialect-specific prefix.</summary>
        public string CreateParameter(string name) => $"{ParameterPrefix}{name}";

        /// <summary>Escapes special characters in string values.</summary>
        private string EscapeString(string value) => value.Replace(StringLeft, StringLeft + StringLeft);

        /// <summary>Gets database type (simplified version)</summary>
        public string DatabaseType => GetDatabaseInfo().Name;

        /// <summary>Gets database type enum (type-safe version)</summary>
        public Annotations.SqlDefineTypes DbType => GetDatabaseInfo().Type;

        /// <summary>
        /// Determines database type and name from dialect configuration.
        /// </summary>
        /// <remarks>
        /// <para><strong>Ambiguity Note:</strong> SQLite and SQL Server share identical syntax ([brackets], @ prefix).</para>
        /// <para>This method returns "SqlServer" for both cases. To distinguish between them:</para>
        /// <list type="bullet">
        /// <item><description>Use <see cref="SqlDefine.SQLite"/> or <see cref="SqlDefine.SqlServer"/> explicitly</description></item>
        /// <item><description>Compare dialect instances using reference equality</description></item>
        /// </list>
        /// <para><strong>Dialect Signatures:</strong></para>
        /// <list type="bullet">
        /// <item><description>MySQL: `backticks` + @</description></item>
        /// <item><description>SQL Server/SQLite: [brackets] + @</description></item>
        /// <item><description>PostgreSQL: "quotes" + $</description></item>
        /// <item><description>Oracle: "quotes" + :</description></item>
        /// <item><description>DB2: "quotes" + ?</description></item>
        /// </list>
        /// </remarks>
        /// <returns>A tuple containing the database name and type enum.</returns>
        /// <exception cref="NotSupportedException">Thrown when the dialect configuration is not recognized.</exception>
        private (string Name, Annotations.SqlDefineTypes Type) GetDatabaseInfo() =>
            (ColumnLeft, ColumnRight, ParameterPrefix) switch
            {
                ("`", "`", "@") => ("MySql", Annotations.SqlDefineTypes.MySql),
                ("[", "]", "@") => ("SqlServer", Annotations.SqlDefineTypes.SqlServer), // Ambiguous: also matches SQLite
                ("\"", "\"", "$") => ("PostgreSql", Annotations.SqlDefineTypes.PostgreSql),
                ("\"", "\"", ":") => ("Oracle", Annotations.SqlDefineTypes.Oracle),
                ("\"", "\"", "?") => ("DB2", Annotations.SqlDefineTypes.DB2),
                _ => throw new NotSupportedException($"Unknown dialect configuration: ColumnLeft='{ColumnLeft}', ColumnRight='{ColumnRight}', ParameterPrefix='{ParameterPrefix}'")
            };

        /// <summary>Gets the string concatenation syntax for the current database dialect.</summary>
        /// <param name="parts">The string parts to concatenate.</param>
        /// <returns>A SQL expression that concatenates the specified parts.</returns>
        /// <remarks>
        /// <para><strong>Dialect-Specific Syntax:</strong></para>
        /// <list type="bullet">
        /// <item><description>SQL Server: part1 + part2 + part3</description></item>
        /// <item><description>PostgreSQL/Oracle: part1 || part2 || part3</description></item>
        /// <item><description>MySQL/SQLite/DB2: CONCAT(part1, part2, part3)</description></item>
        /// </list>
        /// </remarks>
        public string GetConcatFunction(params string[] parts) => DatabaseType switch
        {
            "SqlServer" => string.Join(" + ", parts),
            "PostgreSql" or "Oracle" => string.Join(" || ", parts),
            _ => $"CONCAT({string.Join(", ", parts)})"
        };
    }
}
