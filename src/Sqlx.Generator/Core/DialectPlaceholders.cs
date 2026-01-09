// -----------------------------------------------------------------------
// <copyright file="DialectPlaceholders.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

/// <summary>
/// Defines SQL dialect-specific placeholders that are replaced based on the target database.
/// </summary>
internal static class DialectPlaceholders
{
    /// <summary>
    /// Placeholder for table name. Will be replaced with the actual table name.
    /// Example: {{table}} -> users
    /// </summary>
    public const string Table = "{{table}}";

    /// <summary>
    /// Placeholder for column list. Will be replaced with the entity's column names.
    /// Example: {{columns}} -> id, username, email, age
    /// </summary>
    public const string Columns = "{{columns}}";

    /// <summary>
    /// Placeholder for RETURNING clause to get the inserted ID.
    /// - PostgreSQL: RETURNING id
    /// - MySQL: (empty, uses LAST_INSERT_ID)
    /// - SQL Server: (empty, uses SCOPE_IDENTITY)
    /// - SQLite: (empty, uses last_insert_rowid)
    /// </summary>
    public const string ReturningId = "{{returning_id}}";

    /// <summary>
    /// Placeholder for LIMIT clause.
    /// - PostgreSQL/MySQL/SQLite: LIMIT @limit
    /// - SQL Server: TOP (@limit)
    /// </summary>
    public const string Limit = "{{limit}}";

    /// <summary>
    /// Placeholder for OFFSET clause.
    /// - PostgreSQL/MySQL/SQLite: OFFSET @offset
    /// - SQL Server: OFFSET @offset ROWS
    /// </summary>
    public const string Offset = "{{offset}}";

    /// <summary>
    /// Placeholder for boolean TRUE value.
    /// - PostgreSQL: true
    /// - MySQL/SQL Server/SQLite: 1
    /// </summary>
    public const string BoolTrue = "{{bool_true}}";

    /// <summary>
    /// Placeholder for boolean FALSE value.
    /// - PostgreSQL: false
    /// - MySQL/SQL Server/SQLite: 0
    /// </summary>
    public const string BoolFalse = "{{bool_false}}";

    /// <summary>
    /// Placeholder for LIMIT with OFFSET combined.
    /// - PostgreSQL/MySQL/SQLite: LIMIT @limit OFFSET @offset
    /// - SQL Server: ORDER BY (required) OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY
    /// </summary>
    public const string LimitOffset = "{{limit_offset}}";

    /// <summary>
    /// Placeholder for current timestamp.
    /// - PostgreSQL: CURRENT_TIMESTAMP
    /// - MySQL: CURRENT_TIMESTAMP
    /// - SQL Server: GETDATE()
    /// - SQLite: CURRENT_TIMESTAMP
    /// </summary>
    public const string CurrentTimestamp = "{{current_timestamp}}";

    /// <summary>
    /// Placeholder for string concatenation operator.
    /// - PostgreSQL: ||
    /// - MySQL: CONCAT()
    /// - SQL Server: +
    /// - SQLite: ||
    /// </summary>
    public const string Concat = "{{concat}}";

    /// <summary>
    /// Placeholder for dynamic column name.
    /// Used in methods like GetDistinctValuesAsync where column name is a runtime parameter.
    /// Example: {{column}} -> runtime validated column name
    /// </summary>
    public const string Column = "{{column}}";

    /// <summary>
    /// Placeholder for ORDER BY clause.
    /// Example: {{orderby}} -> ORDER BY column_name
    /// </summary>
    public const string OrderBy = "{{orderby}}";

    /// <summary>
    /// Placeholder for WHERE clause.
    /// Example: {{where}} -> WHERE condition
    /// </summary>
    public const string Where = "{{where}}";

    /// <summary>
    /// Placeholder for SET clause in UPDATE statements.
    /// Example: {{set}} -> SET column1 = @value1, column2 = @value2
    /// </summary>
    public const string Set = "{{set}}";

    /// <summary>
    /// Placeholder for VALUES clause in INSERT statements.
    /// Example: {{values}} -> (@value1, @value2, ...)
    /// </summary>
    public const string Values = "{{values}}";

    /// <summary>
    /// All supported placeholders for validation.
    /// </summary>
    public static readonly string[] All = new[]
    {
        Table,
        Columns,
        ReturningId,
        Limit,
        Offset,
        BoolTrue,
        BoolFalse,
        LimitOffset,
        CurrentTimestamp,
        Concat,
        Column,
        OrderBy,
        Where,
        Set,
        Values
    };

    /// <summary>
    /// Checks if a SQL template contains any dialect-specific placeholders.
    /// </summary>
    /// <param name="sqlTemplate">The SQL template to check.</param>
    /// <returns>True if the template contains placeholders; otherwise, false.</returns>
    public static bool ContainsPlaceholders(string sqlTemplate)
    {
        if (string.IsNullOrEmpty(sqlTemplate))
            return false;

        foreach (var placeholder in All)
        {
            if (sqlTemplate.Contains(placeholder))
                return true;
        }

        return false;
    }
}

