// -----------------------------------------------------------------------
// <copyright file="IDatabaseDialectProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.SqlGen;

namespace Sqlx.Generator.Core;

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
    SqlDefineTypes DialectType { get; }

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
}

