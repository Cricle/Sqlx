// <copyright file="TablePlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

/// <summary>
/// Handles {{table}} placeholder. Always static.
/// </summary>
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
    {
        return QuoteIdentifier(context.Dialect, context.TableName);
    }
}
