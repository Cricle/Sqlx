// -----------------------------------------------------------------------
// <copyright file="SqlDefine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// Provides database dialect-specific SQL formatting definitions.
    /// </summary>
    public static class SqlDefine
    {
        /// <summary>
        /// MySQL dialect configuration with backtick column wrapping and @ parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) MySql = ("`", "`", "'", "'", "@");

        /// <summary>
        /// SQL Server dialect configuration with square bracket column wrapping and @ parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) SqlServer = ("[", "]", "'", "'", "@");

        /// <summary>
        /// PostgreSQL dialect configuration with double quote column wrapping and $ parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) PgSql = ("\"", "\"", "'", "'", "$");

        /// <summary>
        /// Oracle dialect configuration with double quote column wrapping and : parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) Oracle = ("\"", "\"", "'", "'", ":");

        /// <summary>
        /// DB2 dialect configuration with double quote column wrapping and ? parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) DB2 = ("\"", "\"", "'", "'", "?");

        /// <summary>
        /// SQLite dialect configuration with square bracket column wrapping and @ parameter prefix.
        /// </summary>
        public static readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) Sqlite = ("[", "]", "'", "'", "@");
    }
}
