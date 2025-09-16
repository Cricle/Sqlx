// -----------------------------------------------------------------------
// <copyright file="SqlDefineAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// SQL dialect types enumeration.
    /// </summary>
    public enum SqlDefineTypes
    {
        /// <summary>MySQL database</summary>
        MySql = 0,
        /// <summary>SQL Server database</summary>
        SqlServer = 1,
        /// <summary>PostgreSQL database</summary>
        PostgreSql = 2,
        /// <summary>Oracle database</summary>
        Oracle = 3,
        /// <summary>DB2 database</summary>
        DB2 = 4,
        /// <summary>SQLite database</summary>
        SQLite = 5,
    }
    /// <summary>
    /// Specifies the database dialect for SQL generation.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method |
        System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlDefineAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlDefineAttribute"/> class with a predefined dialect enum.
        /// </summary>
        /// <param name="dialectType">The database dialect type.</param>
        public SqlDefineAttribute(SqlDefineTypes dialectType)
        {
            DialectType = dialectType;
            DialectName = dialectType.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlDefineAttribute"/> class with a predefined dialect.
        /// </summary>
        /// <param name="dialectName">The database dialect name (MySql, SqlServer, PostgreSql, Oracle, DB2, SQLite).</param>
        public SqlDefineAttribute(string dialectName)
        {
            DialectName = dialectName ?? throw new System.ArgumentNullException(nameof(dialectName));
            // Try to parse the string to enum for backwards compatibility
            if (System.Enum.TryParse<SqlDefineTypes>(dialectName, true, out var parsedType))
            {
                DialectType = parsedType;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlDefineAttribute"/> class with custom dialect settings.
        /// </summary>
        /// <param name="columnLeft">Left column delimiter.</param>
        /// <param name="columnRight">Right column delimiter.</param>
        /// <param name="stringLeft">Left string delimiter.</param>
        /// <param name="stringRight">Right string delimiter.</param>
        /// <param name="parameterPrefix">Parameter prefix.</param>
        public SqlDefineAttribute(string columnLeft, string columnRight, string stringLeft, string stringRight, string parameterPrefix)
        {
            ColumnLeft = columnLeft ?? throw new System.ArgumentNullException(nameof(columnLeft));
            ColumnRight = columnRight ?? throw new System.ArgumentNullException(nameof(columnRight));
            StringLeft = stringLeft ?? throw new System.ArgumentNullException(nameof(stringLeft));
            StringRight = stringRight ?? throw new System.ArgumentNullException(nameof(stringRight));
            ParameterPrefix = parameterPrefix ?? throw new System.ArgumentNullException(nameof(parameterPrefix));
        }

        /// <summary>
        /// Gets the database dialect type.
        /// </summary>
        public SqlDefineTypes? DialectType { get; }

        /// <summary>
        /// Gets the database dialect name.
        /// </summary>
        public string? DialectName { get; }

        /// <summary>
        /// Gets the left column delimiter.
        /// </summary>
        public string? ColumnLeft { get; }

        /// <summary>
        /// Gets the right column delimiter.
        /// </summary>
        public string? ColumnRight { get; }

        /// <summary>
        /// Gets the left string delimiter.
        /// </summary>
        public string? StringLeft { get; }

        /// <summary>
        /// Gets the right string delimiter.
        /// </summary>
        public string? StringRight { get; }

        /// <summary>
        /// Gets the parameter prefix.
        /// </summary>
        public string? ParameterPrefix { get; }
    }
}
