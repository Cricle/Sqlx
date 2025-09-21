namespace Sqlx
{
    /// <summary>
    /// Database dialect definitions for mainstream databases (AOT-friendly)
    /// </summary>
    public static class SqlDefine
    {
        /// <summary>SQL Server: [column] with @ parameters</summary>
        public static readonly SqlDialect SqlServer = new("[", "]", "'", "'", "@");

        /// <summary>MySQL: `column` with @ parameters</summary>
        public static readonly SqlDialect MySql = new("`", "`", "'", "'", "@");

        /// <summary>PostgreSQL: "column" with $ parameters</summary>
        public static readonly SqlDialect PostgreSql = new("\"", "\"", "'", "'", "$");

        /// <summary>PostgreSQL: "column" with $ parameters (alias)</summary>
        public static readonly SqlDialect PgSql = PostgreSql;


        /// <summary>SQLite: [column] with $ parameters</summary>
        public static readonly SqlDialect SQLite = new("[", "]", "'", "'", "$");

        /// <summary>SQLite: [column] with $ parameters (alias)</summary>
        public static readonly SqlDialect Sqlite = SQLite;

        /// <summary>Oracle: "column" with : parameters</summary>
        public static readonly SqlDialect Oracle = new("\"", "\"", "'", "'", ":");

        /// <summary>DB2: "column" with ? parameters</summary>
        public static readonly SqlDialect DB2 = new("\"", "\"", "'", "'", "?");

        /// <summary>Gets SQL dialect by database type</summary>
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
        public string DatabaseType
        {
            get
            {
                return ColumnLeft switch
                {
                    "`" when ColumnRight == "`" && ParameterPrefix == "@" => "MySql",
                    "[" when ColumnRight == "]" && ParameterPrefix == "@" => "SqlServer",
                    "\"" when ColumnRight == "\"" && ParameterPrefix == "$" => "PostgreSql",
                    "[" when ColumnRight == "]" && ParameterPrefix == "$" => "SQLite",
                    "\"" when ColumnRight == "\"" && ParameterPrefix == ":" => "Oracle",
                    "\"" when ColumnRight == "\"" && ParameterPrefix == "?" => "DB2",
                    _ => throw new NotSupportedException(ColumnLeft)
                };
            }
        }

        /// <summary>Gets database type enum (type-safe version)</summary>
        public Annotations.SqlDefineTypes DbType
        {
            get
            {
                return DatabaseType switch
                {
                    "MySql" => Annotations.SqlDefineTypes.MySql,
                    "SqlServer" => Annotations.SqlDefineTypes.SqlServer,
                    "PostgreSql" => Annotations.SqlDefineTypes.PostgreSql,
                    "SQLite" => Annotations.SqlDefineTypes.SQLite,
                    "Oracle" => Annotations.SqlDefineTypes.Oracle,
                    "DB2" => Annotations.SqlDefineTypes.DB2,
                    _ => throw new NotSupportedException(DatabaseType)
                };
            }
        }

        /// <summary>Gets string concatenation syntax</summary>
        public string GetConcatFunction(params string[] parts) => DatabaseType switch
        {
            "SqlServer" => string.Join(" + ", parts),
            "MySql" or "SQLite" => $"CONCAT({string.Join(", ", parts)})",
            "PostgreSql" => string.Join(" || ", parts),
            "Oracle" => string.Join(" || ", parts),
            "DB2" => $"CONCAT({string.Join(", ", parts)})",
            _ => $"CONCAT({string.Join(", ", parts)})"
        };

        /// <summary>Gets pagination syntax</summary>
        public string GetLimitClause(int count, int offset = 0) => DatabaseType switch
        {
            "SqlServer" => offset > 0 ? $"OFFSET {offset} ROWS FETCH NEXT {count} ROWS ONLY" : $"TOP {count}",
            _ => $"LIMIT {count}" + (offset > 0 ? $" OFFSET {offset}" : "")
        };
    }
}
