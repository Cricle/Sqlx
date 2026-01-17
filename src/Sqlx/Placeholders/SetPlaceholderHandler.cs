// <copyright file="SetPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles the {{set}} placeholder for generating UPDATE SET clauses.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a comma-separated list of column=parameter assignments.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the SET clause</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: UPDATE users SET {{set --exclude Id}} WHERE id = @id
/// // Output:   UPDATE users SET [name] = @name, [email] = @email WHERE id = @id
/// </code>
/// </example>
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
        var columns = FilterColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => $"{context.Dialect.WrapColumn(c.Name)} = {context.Dialect.CreateParameter(c.Name)}"));
    }
}
