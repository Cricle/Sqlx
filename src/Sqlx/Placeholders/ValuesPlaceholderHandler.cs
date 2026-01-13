// <copyright file="ValuesPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles {{values}} placeholder. Always static.
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
        var columns = FilterColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => "@" + c.Name));
    }
}
