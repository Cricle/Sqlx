// <copyright file="ColumnsPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles {{columns}} placeholder. Always static.
/// Supports --exclude option.
/// </summary>
public sealed class ColumnsPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ColumnsPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "columns";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = GetFilteredColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => QuoteIdentifier(context.Dialect, c.Name)));
    }
}
