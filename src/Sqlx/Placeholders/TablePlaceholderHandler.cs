// <copyright file="TablePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

/// <summary>
/// Handles the {{table}} placeholder for generating quoted table names.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a dialect-specific quoted table name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT * FROM {{table}}
/// // SQLite:   SELECT * FROM [users]
/// // MySQL:    SELECT * FROM `users`
/// // PostgreSQL: SELECT * FROM "users"
/// </code>
/// </example>
public sealed class TablePlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static TablePlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "table";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
        => context.Dialect.WrapColumn(context.TableName);
}
