// <copyright file="SetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles {{set}} placeholder. Always static.
/// Generates SET clause like column = @column.
/// Supports --exclude option.
/// </summary>
public sealed class SetPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static SetPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "set";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = GetFilteredColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c =>
            $"{QuoteIdentifier(context.Dialect, c.Name)} = {context.Dialect.ParameterPrefix}{c.Name}"));
    }
}
