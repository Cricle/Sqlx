// <copyright file="ColumnsPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles the {{columns}} placeholder for generating SELECT column lists.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a comma-separated list of quoted column names.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the list</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: SELECT {{columns}} FROM users
/// // Output:   SELECT [id], [name], [email] FROM users
/// 
/// // Template: SELECT {{columns --exclude Id}} FROM users
/// // Output:   SELECT [name], [email] FROM users
/// </code>
/// </example>
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
        var columns = FilterColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => context.Dialect.WrapColumn(c.Name)));
    }
}
