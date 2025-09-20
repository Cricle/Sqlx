// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core
{
    /// <summary>
    /// Internal SQL dialect definition for code generation.
    /// </summary>
    public readonly struct SqlDefine
    {
        /// <summary>
        /// Gets column left delimiter
        /// </summary>
        public string ColumnLeft { get; }
        /// <summary>
        /// Gets column right delimiter
        /// </summary>
        public string ColumnRight { get; }
        /// <summary>
        /// Gets string left delimiter
        /// </summary>
        public string StringLeft { get; }
        /// <summary>
        /// Gets string right delimiter
        /// </summary>
        public string StringRight { get; }
        /// <summary>
        /// Gets parameter prefix
        /// </summary>
        public string ParameterPrefix { get; }

        /// <summary>
        /// Initializes a new instance of the SqlDefine struct
        /// </summary>
        /// <param name="columnLeft">Column left delimiter</param>
        /// <param name="columnRight">Column right delimiter</param>
        /// <param name="stringLeft">String left delimiter</param>
        /// <param name="stringRight">String right delimiter</param>
        /// <param name="parameterPrefix">Parameter prefix</param>
        public SqlDefine(string columnLeft, string columnRight, string stringLeft, string stringRight, string parameterPrefix)
        {
            ColumnLeft = columnLeft;
            ColumnRight = columnRight;
            StringLeft = stringLeft;
            StringRight = stringRight;
            ParameterPrefix = parameterPrefix;
        }

        /// <summary>
        /// Get SQL dialect definition for MySQL database.
        /// </summary>
        public static readonly SqlDefine MySql = new SqlDefine("`", "`", "'", "'", "@");
        /// <summary>
        /// Get SQL dialect definition for SQL Server database.
        /// </summary>
        public static readonly SqlDefine SqlServer = new SqlDefine("[", "]", "'", "'", "@");
        /// <summary>
        /// Get SQL dialect definition for PostgreSQL database.
        /// </summary>
        public static readonly SqlDefine PostgreSql = new SqlDefine("\"", "\"", "'", "'", "$");
        /// <summary>
        /// Get SQL dialect definition for SQLite database.
        /// </summary>
        public static readonly SqlDefine SQLite = new SqlDefine("[", "]", "'", "'", "$");

        /// <summary>
        /// Get SQL dialect definition for PostgreSQL database (alias).
        /// </summary>
        public static readonly SqlDefine PgSql = PostgreSql;

        /// <summary>
        /// Get SQL dialect definition for Oracle database.
        /// </summary>
        public static readonly SqlDefine Oracle = new SqlDefine("\"", "\"", "'", "'", ":");

        /// <summary>
        /// Get SQL dialect definition for DB2 database.
        /// </summary>
        public static readonly SqlDefine DB2 = new SqlDefine("\"", "\"", "'", "'", "?");

        /// <summary>
        /// Wrap the specified string using string delimiters.
        /// </summary>
        /// <param name="input">The string to wrap.</param>
        /// <returns>The wrapped string.</returns>
        public string WrapString(string input) => $"{StringLeft}{input}{StringRight}";
        /// <summary>
        /// Wrap the specified column name using column delimiters.
        /// </summary>
        /// <param name="input">The column name to wrap.</param>
        /// <returns>The wrapped column name.</returns>
        public string WrapColumn(string input) => $"{ColumnLeft}{input}{ColumnRight}";

        /// <summary>
        /// Deconstruct the SqlDefine structure into its component parts.
        /// </summary>
        /// <param name="columnLeft">Left delimiter for column names.</param>
        /// <param name="columnRight">Right delimiter for column names.</param>
        /// <param name="stringLeft">Left delimiter for strings.</param>
        /// <param name="stringRight">Right delimiter for strings.</param>
        /// <param name="parameterPrefix">Prefix for parameters.</param>
        public void Deconstruct(out string columnLeft, out string columnRight, out string stringLeft, out string stringRight, out string parameterPrefix)
        {
            columnLeft = ColumnLeft;
            columnRight = ColumnRight;
            stringLeft = StringLeft;
            stringRight = StringRight;
            parameterPrefix = ParameterPrefix;
        }
    }

    /// <summary>
    /// SQL dialect types enumeration for code generation.
    /// </summary>
    public enum SqlDefineTypes
    {
        /// <summary>
        /// MySQL database dialect type.
        /// </summary>
        MySql = 0,
        /// <summary>
        /// SQL Server database dialect type.
        /// </summary>
        SqlServer = 1,
        /// <summary>
        /// PostgreSQL database dialect type.
        /// </summary>
        PostgreSql = 2,
        /// <summary>
        /// Oracle database dialect type.
        /// </summary>
        Oracle = 3,
        /// <summary>
        /// DB2 database dialect type.
        /// </summary>
        DB2 = 4,
        /// <summary>
        /// SQLite database dialect type.
        /// </summary>
        SQLite = 5,
    }
}

