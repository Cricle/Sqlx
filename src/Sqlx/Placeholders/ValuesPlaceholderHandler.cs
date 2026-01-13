// <copyright file="ValuesPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles {{values}} placeholder. Always static.
/// Generates parameter placeholders like @column_name.
/// Supports --exclude option.
/// </summary>
public sealed class ValuesPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValuesPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "values";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = GetFilteredColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => $"{context.Dialect.ParameterPrefix}{c.Name}"));
    }
}
