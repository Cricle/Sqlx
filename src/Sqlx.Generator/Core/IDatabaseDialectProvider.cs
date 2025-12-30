// -----------------------------------------------------------------------
// <copyright file="IDatabaseDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;

namespace Sqlx.Generator;

/// <summary>
/// Provides database-specific SQL generation and syntax handling.
/// </summary>
internal interface IDatabaseDialectProvider
{
    /// <summary>
    /// Gets the SQL definition for this dialect.
    /// </summary>
    SqlDefine SqlDefine { get; }

    /// <summary>
    /// Gets the dialect type.
    /// </summary>
    Generator.SqlDefineTypes DialectType { get; }

    /// <summary>
    /// Generates a LIMIT clause for pagination.
    /// </summary>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <param name="offset">The number of rows to skip.</param>
    /// <returns>The SQL LIMIT clause.</returns>
    string GenerateLimitClause(int? limit, int? offset);

    /// <summary>
    /// Generates an INSERT statement with RETURNING clause for getting the inserted ID.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <returns>The SQL INSERT statement with RETURNING clause.</returns>
    string GenerateInsertWithReturning(string tableName, string[] columns);

    /// <summary>
    /// Generates a batch INSERT statement optimized for the specific database.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <param name="batchSize">The batch size.</param>
    /// <returns>The SQL batch INSERT statement.</returns>
    string GenerateBatchInsert(string tableName, string[] columns, int batchSize);

    /// <summary>
    /// Generates an UPSERT (INSERT ... ON CONFLICT UPDATE) statement.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <param name="keyColumns">The key columns for conflict detection.</param>
    /// <returns>The SQL UPSERT statement.</returns>
    string GenerateUpsert(string tableName, string[] columns, string[] keyColumns);

    /// <summary>
    /// Converts a .NET type to the appropriate database type name.
    /// </summary>
    /// <param name="dotNetType">The .NET type.</param>
    /// <returns>The database-specific type name.</returns>
    string GetDatabaseTypeName(System.Type dotNetType);

    /// <summary>
    /// Formats a date/time value for the specific database.
    /// </summary>
    /// <param name="dateTime">The date/time value.</param>
    /// <returns>The formatted date/time string.</returns>
    string FormatDateTime(System.DateTime dateTime);

    /// <summary>
    /// Gets the syntax for getting the current date/time.
    /// </summary>
    /// <returns>The SQL expression for current date/time.</returns>
    string GetCurrentDateTimeSyntax();

    /// <summary>
    /// Gets the syntax for string concatenation.
    /// </summary>
    /// <param name="expressions">The expressions to concatenate.</param>
    /// <returns>The SQL string concatenation expression.</returns>
    string GetConcatenationSyntax(params string[] expressions);

    /// <summary>
    /// Replaces dialect-specific placeholders in a SQL template.
    /// </summary>
    /// <param name="sqlTemplate">The SQL template containing placeholders.</param>
    /// <param name="tableName">The table name to replace {{table}} placeholder.</param>
    /// <param name="columns">The column names to replace {{columns}} placeholder.</param>
    /// <returns>The SQL template with placeholders replaced.</returns>
    string ReplacePlaceholders(string sqlTemplate, string? tableName = null, string[]? columns = null);

    /// <summary>
    /// Gets the SQL fragment for returning the inserted ID.
    /// </summary>
    /// <returns>The RETURNING clause or empty string.</returns>
    string GetReturningIdClause();

    /// <summary>
    /// Gets the boolean TRUE literal for this dialect.
    /// </summary>
    /// <returns>The boolean TRUE literal.</returns>
    string GetBoolTrueLiteral();

    /// <summary>
    /// Gets the boolean FALSE literal for this dialect.
    /// </summary>
    /// <returns>The boolean FALSE literal.</returns>
    string GetBoolFalseLiteral();

    /// <summary>
    /// Generates a combined LIMIT and OFFSET clause.
    /// </summary>
    /// <param name="limitParam">The limit parameter name.</param>
    /// <param name="offsetParam">The offset parameter name.</param>
    /// <param name="requiresOrderBy">Whether ORDER BY is required (true for SQL Server).</param>
    /// <returns>The SQL LIMIT/OFFSET clause.</returns>
    string GenerateLimitOffsetClause(string limitParam, string offsetParam, out bool requiresOrderBy);
}

