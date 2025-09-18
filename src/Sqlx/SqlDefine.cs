namespace Sqlx
{
    /// <summary>
    /// Database types enumeration for type-safe database selection
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>Microsoft SQL Server</summary>
        SqlServer,
        /// <summary>MySQL database</summary>
        MySql,
        /// <summary>PostgreSQL database</summary>
        PostgreSql,
        /// <summary>SQLite database</summary>
        SQLite,
        /// <summary>Oracle database</summary>
        Oracle,
        /// <summary>DB2 database</summary>
        DB2
    }

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
        public static SqlDialect GetDialect(DatabaseType databaseType) => databaseType switch
        {
            DatabaseType.MySql => MySql,
            DatabaseType.SqlServer => SqlServer,
            DatabaseType.PostgreSql => PostgreSql,
            DatabaseType.SQLite => SQLite,
            DatabaseType.Oracle => Oracle,
            DatabaseType.DB2 => DB2,
            _ => SqlServer // Default to SQL Server
        };

        /// <summary>
        /// Gets database dialect for mainstream databases (string version for backward compatibility)
        /// </summary>
        public static SqlDialect GetDialect(string dialectName) => dialectName?.ToLowerInvariant() switch
        {
            "mysql" => MySql,
            "sqlserver" or "mssql" => SqlServer,
            "postgresql" or "postgres" or "pgsql" => PostgreSql,
            "sqlite" => SQLite,
            "oracle" => Oracle,
            "db2" => DB2,
            _ => SqlServer // Default to SQL Server
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
                if (ColumnLeft == "`" && ColumnRight == "`" && ParameterPrefix == "@") return "MySql";
                if (ColumnLeft == "[" && ColumnRight == "]" && ParameterPrefix == "@") return "SqlServer";
                if (ColumnLeft == "\"" && ColumnRight == "\"" && ParameterPrefix == "$") return "PostgreSql";
                if (ColumnLeft == "[" && ColumnRight == "]" && ParameterPrefix == "$") return "SQLite";
                if (ColumnLeft == "\"" && ColumnRight == "\"" && ParameterPrefix == ":") return "Oracle";
                if (ColumnLeft == "\"" && ColumnRight == "\"" && ParameterPrefix == "?") return "DB2";
                return "SqlServer";
            }
        }

        /// <summary>Gets database type enum (type-safe version)</summary>
        public DatabaseType DbType
        {
            get
            {
                var dbType = DatabaseType;
                if (dbType == "MySql") return Sqlx.DatabaseType.MySql;
                if (dbType == "SqlServer") return Sqlx.DatabaseType.SqlServer;
                if (dbType == "PostgreSql") return Sqlx.DatabaseType.PostgreSql;
                if (dbType == "SQLite") return Sqlx.DatabaseType.SQLite;
                if (dbType == "Oracle") return Sqlx.DatabaseType.Oracle;
                if (dbType == "DB2") return Sqlx.DatabaseType.DB2;
                return Sqlx.DatabaseType.SqlServer;
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
