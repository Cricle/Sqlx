// -----------------------------------------------------------------------
// <copyright file="SqlDefineAttribute.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Specifies the database dialect for SQL generation.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method |
        System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SqlDefineAttribute : System.Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlDefineAttribute"/> class with a predefined dialect.
        /// </summary>
        /// <param name="dialectName">The database dialect name (MySql, SqlServer, PostgreSql, Oracle, DB2, SQLite).</param>
        public SqlDefineAttribute(string dialectName)
        {
            DialectName = dialectName ?? throw new System.ArgumentNullException(nameof(dialectName));
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
