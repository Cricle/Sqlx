// <copyright file="PlaceholderContext.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;

/// <summary>
/// Context for placeholder processing.
/// </summary>
public sealed class PlaceholderContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderContext"/> class.
    /// </summary>
    public PlaceholderContext(SqlDialect dialect, string tableName, IReadOnlyList<ColumnMeta> columns)
    {
        Dialect = dialect;
        TableName = tableName;
        Columns = columns;
    }

    /// <summary>
    /// Gets the SQL dialect.
    /// </summary>
    public SqlDialect Dialect { get; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the entity columns.
    /// </summary>
    public IReadOnlyList<ColumnMeta> Columns { get; }
}
